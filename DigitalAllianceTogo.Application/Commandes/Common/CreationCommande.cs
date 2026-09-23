using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

namespace DigitalAllianceTogo.Application.Commandes.Common
{
    /// <summary>
    /// Construction d'une nouvelle commande (§8), partagée par l'acceptation d'un devis et la
    /// commande directe depuis le panier : adresse figée (snapshot), téléphone de contact,
    /// version 1. La commande ne réserve PAS le stock : cela se fera après paiement.
    /// </summary>
    internal static class CreationCommande
    {
        public record LigneNouvelle(Guid ProduitId, int Quantite, decimal PrixUnitaire, decimal Remise, decimal Total);

        /// <summary>Adresse du client (vérifiée) et téléphone de contact (celui de la fiche par défaut).</summary>
        public static async Task<(Adresse Adresse, string Telephone)> ResoudreLivraisonAsync(
            IApplicationDbContext context, Guid clientId, Guid adresseId, string? telephone, CancellationToken cancellationToken)
        {
            var adresse = await context.Adresses.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == adresseId && a.ClientId == clientId, cancellationToken)
                ?? throw new NotFoundException("Adresse", adresseId);

            if (string.IsNullOrWhiteSpace(telephone))
            {
                telephone = await context.Clients
                    .Where(c => c.Id == clientId)
                    .Select(c => c.Telephone)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(telephone))
                throw new ConflictException("Un téléphone de contact est nécessaire pour la livraison.");

            return (adresse, telephone.Trim());
        }

        public static CommandeEntity Construire(
            Guid clientId, Adresse adresse, string telephone, IEnumerable<LigneNouvelle> lignes,
            decimal sousTotal, decimal remise, decimal total, string motif, Guid? devisOrigineId)
        {
            var commande = new CommandeEntity
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("CMD"),
                Statut = StatutCommande.CommandeCreee,
                DateCreation = DateTime.UtcNow,
                ClientId = clientId,
                DevisOrigineId = devisOrigineId,
                VersionActive = 1,
                AdresseLivraison = new AdresseLivraisonCommande
                {
                    Id = Guid.NewGuid(),
                    Ligne1 = adresse.Ligne1,
                    Ligne2 = adresse.Ligne2,
                    Ville = adresse.Ville,
                    Pays = adresse.Pays,
                    CodePostal = adresse.CodePostal,
                    TelephoneContact = telephone
                }
            };

            var version = new VersionCommande
            {
                Id = Guid.NewGuid(),
                NumeroVersion = 1,
                DateCreation = DateTime.UtcNow,
                MotifModification = motif,
                SousTotal = sousTotal,
                Remise = remise,
                Total = total,
                Active = true
            };
            foreach (var ligne in lignes)
            {
                version.Lignes.Add(new LigneCommande
                {
                    Id = Guid.NewGuid(),
                    ProduitId = ligne.ProduitId,
                    Quantite = ligne.Quantite,
                    PrixUnitaire = ligne.PrixUnitaire,
                    Remise = ligne.Remise,
                    Total = ligne.Total
                });
            }
            commande.Versions.Add(version);
            return commande;
        }
    }
}
