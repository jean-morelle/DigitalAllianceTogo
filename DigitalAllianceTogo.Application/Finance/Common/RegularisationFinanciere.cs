using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

namespace DigitalAllianceTogo.Application.Finance.Common
{
    /// <summary>
    /// Régularisation financière d'une commande payée puis annulée ou refusée (§17, §22-23)
    /// et règles de clôture. N'enregistre rien : l'appelant fait le SaveChanges.
    /// </summary>
    internal static class RegularisationFinanciere
    {
        private static readonly StatutRemboursement[] RemboursementsNonTermines =
            { StatutRemboursement.EnAttente, StatutRemboursement.Valide, StatutRemboursement.Echoue };

        private static readonly StatutAvoir[] AvoirsNonTermines = { StatutAvoir.EnAttente, StatutAvoir.Valide };

        /// <summary>
        /// Crée la demande de remboursement ou d'avoir (En attente de validation Administrateur)
        /// pour le montant réellement encaissé. Rien si la commande n'a jamais été payée.
        /// </summary>
        public static async Task<decimal> CreerAsync(
            IApplicationDbContext context,
            IAuditService audit,
            CommandeEntity commande,
            ModeRegularisation mode,
            string motif,
            CancellationToken cancellationToken)
        {
            var montant = await context.Paiements
                .Where(p => p.CommandeId == commande.Id && p.Statut == StatutPaiement.Confirme)
                .SumAsync(p => p.Montant, cancellationToken);
            if (montant <= 0)
                return 0;

            var versionId = await context.VersionsCommande
                .Where(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive)
                .Select(v => v.Id)
                .FirstAsync(cancellationToken);

            if (mode == ModeRegularisation.Avoir)
            {
                var avoir = new Avoir
                {
                    Id = Guid.NewGuid(),
                    Reference = DigitalAllianceTogo.Application.Common.References.Generer("AVO"),
                    Montant = montant,
                    DateCreation = DateTime.UtcNow,
                    Statut = StatutAvoir.EnAttente,
                    Motif = Tronquer(motif),
                    CommandeId = commande.Id,
                    VersionCommandeId = versionId
                };
                context.Avoirs.Add(avoir);
                audit.Enregistrer("DemandeAvoir", "Avoir", avoir.Id, apres: new { avoir.Reference, avoir.Montant, avoir.Motif, Commande = commande.Reference });
            }
            else
            {
                var remboursement = new Remboursement
                {
                    Id = Guid.NewGuid(),
                    Reference = DigitalAllianceTogo.Application.Common.References.Generer("RBT"),
                    Montant = montant,
                    DateDemande = DateTime.UtcNow,
                    Statut = StatutRemboursement.EnAttente,
                    Motif = Tronquer(motif),
                    CommandeId = commande.Id,
                    VersionCommandeId = versionId
                };
                context.Remboursements.Add(remboursement);
                audit.Enregistrer("DemandeRemboursement", "Remboursement", remboursement.Id, apres: new { remboursement.Reference, remboursement.Montant, remboursement.Motif, Commande = commande.Reference });
            }
            return montant;
        }

        /// <summary>
        /// Vrai si l'argent du client n'est pas encore régularisé : remboursement en attente,
        /// validé mais non exécuté, ou ÉCHOUÉ ; avoir pas encore mis à disposition.
        /// Évalué sur les entités suivies par EF : on voit les demandes ajoutées et les statuts
        /// modifiés dans la requête en cours, même pas encore enregistrés.
        /// </summary>
        public static async Task<bool> EnCoursAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken)
        {
            // Charger = attacher au contexte ; Local contient alors base + ajouts, avec les valeurs en mémoire
            await context.Remboursements.Where(r => r.CommandeId == commandeId).LoadAsync(cancellationToken);
            await context.Avoirs.Where(a => a.CommandeId == commandeId).LoadAsync(cancellationToken);

            return context.Remboursements.Local.Any(r => r.CommandeId == commandeId && RemboursementsNonTermines.Contains(r.Statut))
                || context.Avoirs.Local.Any(a => a.CommandeId == commandeId && AvoirsNonTermines.Contains(a.Statut));
        }

        /// <summary>
        /// Fin de la partie physique d'une annulation / d'un refus : Annulée, puis
        /// En attente de régularisation financière tant que l'argent n'est pas rendu,
        /// sinon Clôturée directement.
        /// </summary>
        public static async Task TerminerAnnulationAsync(IApplicationDbContext context, IAuditService audit, CommandeEntity commande, CancellationToken cancellationToken)
        {
            var avant = commande.Statut.ToString();
            commande.Statut = StatutCommande.Annulee;
            audit.Enregistrer("Annulation", "Commande", commande.Id, new { Statut = avant }, new { Statut = commande.Statut.ToString() });

            if (await EnCoursAsync(context, commande.Id, cancellationToken))
            {
                commande.Statut = StatutCommande.EnAttenteRegulationFinanciere;
                audit.Enregistrer("AttenteRegularisation", "Commande", commande.Id,
                    new { Statut = StatutCommande.Annulee.ToString() }, new { Statut = commande.Statut.ToString() });
            }
            else
            {
                Cloturer(audit, commande, "Annulée sans conséquence financière restante");
            }
        }

        /// <summary>
        /// Après un remboursement exécuté ou un avoir traité : clôture la commande
        /// si elle n'attendait plus que ça.
        /// </summary>
        public static async Task CloturerSiRegulariseeAsync(IApplicationDbContext context, IAuditService audit, CommandeEntity commande, CancellationToken cancellationToken)
        {
            if (commande.Statut == StatutCommande.EnAttenteRegulationFinanciere
                && !await EnCoursAsync(context, commande.Id, cancellationToken))
                Cloturer(audit, commande, "Régularisation financière terminée");
        }

        /// <summary>Les motifs composés (« Annulation : ... ») ne doivent pas dépasser la colonne (500).</summary>
        public static string Tronquer(string motif) => motif.Length <= 500 ? motif : motif[..500];

        public static void Cloturer(IAuditService audit, CommandeEntity commande, string motif)
        {
            var avant = commande.Statut.ToString();
            commande.Statut = StatutCommande.Cloturee;
            audit.Enregistrer("Cloture", "Commande", commande.Id, new { Statut = avant }, new { Statut = commande.Statut.ToString(), Motif = motif });
        }
    }
}
