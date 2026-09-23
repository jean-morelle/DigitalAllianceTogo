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
            // Ce que le client a versé et qui ne lui a pas déjà été rendu (ex : baisse de prix §21)
            var montant = await SoldeCommande.PayeNetAsync(context, commande.Id, cancellationToken);
            if (montant <= 0)
                return 0;

            var versionId = await context.VersionsCommande
                .Where(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive)
                .Select(v => v.Id)
                .FirstAsync(cancellationToken);

            Ajouter(context, audit, mode, montant, motif, commande.Id, versionId, ticket: null, commande.Reference);
            return montant;
        }

        /// <summary>
        /// SAV irréparable, le client choisit remboursement ou avoir (§26) : montant = ce que le
        /// client a réellement payé pour les unités concernées (prix de la ligne, remise globale
        /// de la version répartie au prorata). La commande n'est pas touchée.
        /// </summary>
        public static async Task<decimal> CreerPourSavAsync(
            IApplicationDbContext context,
            IAuditService audit,
            Domain.Models.SAV.TicketSAV ticket,
            ModeRegularisation mode,
            CancellationToken cancellationToken)
        {
            var ligne = await context.LignesCommande
                .Include(l => l.VersionCommande).ThenInclude(v => v.Lignes)
                .Include(l => l.VersionCommande).ThenInclude(v => v.Commande)
                .FirstAsync(l => l.Id == ticket.LigneCommandeId, cancellationToken);
            var version = ligne.VersionCommande;

            var totalLignes = version.Lignes.Sum(l => l.Total);
            var ratioRemiseGlobale = totalLignes == 0 ? 0 : version.Total / totalLignes;
            var montant = Math.Round(ligne.Total / ligne.Quantite * ticket.Quantite * ratioRemiseGlobale, 0, MidpointRounding.AwayFromZero);

            Ajouter(context, audit, mode, montant, $"SAV {ticket.Reference} : {ticket.Motif}",
                version.CommandeId, version.Id, ticket, version.Commande.Reference);
            return montant;
        }

        private static void Ajouter(
            IApplicationDbContext context, IAuditService audit, ModeRegularisation mode, decimal montant, string motif,
            Guid commandeId, Guid versionId, Domain.Models.SAV.TicketSAV? ticket, string referenceCommande)
        {
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
                    CommandeId = commandeId,
                    VersionCommandeId = versionId,
                    TicketSAVId = ticket?.Id
                };
                context.Avoirs.Add(avoir);
                audit.Enregistrer("DemandeAvoir", "Avoir", avoir.Id, apres: new { avoir.Reference, avoir.Montant, avoir.Motif, Commande = referenceCommande, TicketSAV = ticket?.Reference });
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
                    CommandeId = commandeId,
                    VersionCommandeId = versionId,
                    TicketSAVId = ticket?.Id
                };
                context.Remboursements.Add(remboursement);
                audit.Enregistrer("DemandeRemboursement", "Remboursement", remboursement.Id, apres: new { remboursement.Reference, remboursement.Montant, remboursement.Motif, Commande = referenceCommande, TicketSAV = ticket?.Reference });
            }
        }

        /// <summary>
        /// Vrai si l'argent du client n'est pas encore régularisé : remboursement en attente,
        /// validé mais non exécuté, ou ÉCHOUÉ ; avoir pas encore mis à disposition.
        /// Évalué sur les entités suivies par EF : on voit les demandes ajoutées et les statuts
        /// modifiés dans la requête en cours, même pas encore enregistrés.
        /// Les régularisations d'un SAV ne comptent pas : le SAV est indépendant de la commande (§24).
        /// </summary>
        public static async Task<bool> EnCoursAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken)
        {
            // Charger = attacher au contexte ; Local contient alors base + ajouts, avec les valeurs en mémoire
            await context.Remboursements.Where(r => r.CommandeId == commandeId).LoadAsync(cancellationToken);
            await context.Avoirs.Where(a => a.CommandeId == commandeId).LoadAsync(cancellationToken);

            return context.Remboursements.Local.Any(r => r.CommandeId == commandeId && r.TicketSAVId == null && RemboursementsNonTermines.Contains(r.Statut))
                || context.Avoirs.Local.Any(a => a.CommandeId == commandeId && a.TicketSAVId == null && AvoirsNonTermines.Contains(a.Statut));
        }

        /// <summary>Même règle, pour l'argent décidé dans le cadre d'un ticket SAV.</summary>
        public static async Task<bool> EnCoursPourTicketAsync(IApplicationDbContext context, Guid ticketId, CancellationToken cancellationToken)
        {
            await context.Remboursements.Where(r => r.TicketSAVId == ticketId).LoadAsync(cancellationToken);
            await context.Avoirs.Where(a => a.TicketSAVId == ticketId).LoadAsync(cancellationToken);

            return context.Remboursements.Local.Any(r => r.TicketSAVId == ticketId && RemboursementsNonTermines.Contains(r.Statut))
                || context.Avoirs.Local.Any(a => a.TicketSAVId == ticketId && AvoirsNonTermines.Contains(a.Statut));
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
