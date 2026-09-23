using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Devis;
using Microsoft.EntityFrameworkCore;
using DevisEntity = DigitalAllianceTogo.Domain.Models.Devis.Devis;

namespace DigitalAllianceTogo.Application.Devis.Common
{
    /// <summary>
    /// Règles partagées par les commandes du module Devis.
    /// </summary>
    internal static class DevisHelper
    {
        /// <summary>
        /// Construit les lignes et recalcule SousTotal / Remise / Total du devis.
        /// SousTotal = somme des (quantité × prix catalogue), AVANT toute remise.
        /// Remise    = remises des lignes + remise globale.
        /// Le taux de remise (Remise / SousTotal) sert ensuite au seuil Commercial / Administrateur.
        /// </summary>
        public static async Task AppliquerLignesAsync(
            IApplicationDbContext context,
            DevisEntity devis,
            IReadOnlyCollection<LigneDevisInput> lignes,
            decimal remiseGlobale,
            CancellationToken cancellationToken)
        {
            var produitIds = lignes.Select(l => l.ProduitId).Distinct().ToList();

            var produits = await context.Produits
                .Where(p => produitIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            devis.Lignes.Clear();
            decimal sousTotal = 0, remiseLignes = 0;

            foreach (var ligne in lignes)
            {
                if (!produits.TryGetValue(ligne.ProduitId, out var produit))
                    throw new NotFoundException("Produit", ligne.ProduitId);

                if (!produit.Actif)
                    throw new ConflictException($"Le produit \"{produit.Nom}\" n'est plus disponible à la vente.");

                var montantBrut = produit.Prix * ligne.Quantite;
                if (ligne.Remise > montantBrut)
                    throw new ConflictException($"La remise sur \"{produit.Nom}\" dépasse le montant de la ligne.");

                // Pas d'Id explicite : sur un devis déjà suivi par EF, une clé renseignée ferait
                // croire à une ligne existante (UPDATE au lieu d'INSERT). EF génère l'Id.
                devis.Lignes.Add(new LigneDevis
                {
                    ProduitId = produit.Id,
                    Quantite = ligne.Quantite,
                    PrixUnitaire = produit.Prix, // prix catalogue au moment du devis
                    Remise = ligne.Remise,
                    Total = montantBrut - ligne.Remise
                });

                sousTotal += montantBrut;
                remiseLignes += ligne.Remise;
            }

            if (remiseLignes + remiseGlobale > sousTotal)
                throw new ConflictException("La remise totale dépasse le montant du devis.");

            devis.SousTotal = sousTotal;
            devis.Remise = remiseLignes + remiseGlobale;
            devis.Total = sousTotal - devis.Remise;
        }

        /// <summary>
        /// Charge un devis et vérifie que l'utilisateur courant peut y accéder :
        /// le personnel (Admin/Commercial) voit tout, un Client uniquement ses propres devis.
        /// </summary>
        public static async Task<DevisEntity> ChargerAvecControleAccesAsync(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            Guid devisId,
            CancellationToken cancellationToken,
            bool avecLignes = false)
        {
            IQueryable<DevisEntity> query = context.Devis.Include(d => d.Client);
            if (avecLignes)
                query = query.Include(d => d.Lignes);

            var devis = await query.FirstOrDefaultAsync(d => d.Id == devisId, cancellationToken)
                ?? throw new NotFoundException("Devis", devisId);

            if (!EstPersonnel(currentUser) && devis.Client.UtilisateurId != currentUser.UtilisateurId)
                throw new ForbiddenAccessException();

            return devis;
        }

        public static bool EstPersonnel(ICurrentUserService currentUser) =>
            currentUser.EstDansRole(Roles.Admin) || currentUser.EstDansRole(Roles.Commercial);

        /// <summary>
        /// Un devis envoyé dont la date de validité est dépassée passe en Expiré.
        /// Retourne vrai si le devis vient d'expirer (l'appelant sauvegarde puis refuse l'action).
        /// </summary>
        public static bool MarquerExpireSiDepasse(DevisEntity devis)
        {
            if (devis.Statut == StatutDevis.Envoye && devis.DateValidite < DateTime.UtcNow)
            {
                devis.Statut = StatutDevis.Expire;
                return true;
            }
            return false;
        }

        /// <summary>Instantané léger pour le journal d'audit (avant / après).</summary>
        public static object Instantane(DevisEntity devis) => new
        {
            Statut = devis.Statut.ToString(),
            devis.SousTotal,
            devis.Remise,
            devis.Total,
            devis.TauxRemise,
            devis.ValideParId,
            devis.DateValidite
        };
    }
}
