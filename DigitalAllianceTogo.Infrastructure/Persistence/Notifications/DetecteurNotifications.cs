using System.Globalization;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Finance;
using DigitalAllianceTogo.Domain.Models.Notifications;
using DigitalAllianceTogo.Domain.Models.SAV;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using DevisEntite = DigitalAllianceTogo.Domain.Models.Devis.Devis;
using LivraisonEntite = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Notifications
{
    /// <summary>
    /// Déduit les notifications client des changements de statut en cours d'enregistrement.
    /// Un seul point d'entrée, quel que soit le chemin (écran, tâche automatique, réservation
    /// après réception de stock) : aucun handler ne peut oublier de prévenir le client,
    /// et la notification n'existe que si l'opération est bien enregistrée (même transaction).
    /// </summary>
    internal sealed class DetecteurNotifications
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

        private readonly ApplicationDbContext _context;
        private readonly List<Notification> _nouvelles = new();
        private readonly Dictionary<Guid, (Guid ClientId, string Reference)> _commandes = new();

        public DetecteurNotifications(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AjouterAsync(CancellationToken cancellationToken)
        {
            var entrees = _context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .ToList();

            foreach (var e in entrees)
            {
                switch (e.Entity)
                {
                    case Commande c: Commande(e, c); break;
                    case DevisEntite d: Devis(e, d); break;
                    case LivraisonEntite l: await LivraisonAsync(e, l, cancellationToken); break;
                    case VersionCommande v: await VersionAsync(e, v, cancellationToken); break;
                    case TicketSAV t: TicketSav(e, t); break;
                    case Remboursement r: await RemboursementAsync(e, r, cancellationToken); break;
                    case Avoir a: await AvoirAsync(e, a, cancellationToken); break;
                }
            }

            _context.Notifications.AddRange(_nouvelles);
        }

        private static bool Change<T>(EntityEntry e, string propriete, out T? avant, out T? apres)
        {
            var p = e.Property(propriete);
            avant = e.State == EntityState.Added ? default : (T?)p.OriginalValue;
            apres = (T?)p.CurrentValue;
            return e.State == EntityState.Added || !Equals(avant, apres);
        }

        private void Commande(EntityEntry e, Commande c)
        {
            _commandes[c.Id] = (c.ClientId, c.Reference);
            if (e.State != EntityState.Modified || !Change<StatutCommande>(e, nameof(c.Statut), out var avant, out var apres))
                return;

            var lien = $"/compte/commandes/{c.Id}";
            var aPayer = avant is StatutCommande.CommandeCreee or StatutCommande.PaiementEnAttente or StatutCommande.PaiementEchoue;

            switch (apres)
            {
                case StatutCommande.PaiementConfirme or StatutCommande.StockReserve when aPayer:
                    Ajouter(c.ClientId, "PaiementConfirme", "Paiement confirmé",
                        $"Nous avons bien reçu votre paiement pour la commande {c.Reference}. Nous préparons vos produits.", lien);
                    break;
                case StatutCommande.EnAttenteDisponibilite when aPayer:
                    Ajouter(c.ClientId, "PaiementConfirme", "Paiement confirmé",
                        $"Nous avons bien reçu votre paiement pour la commande {c.Reference}. Certains produits sont en cours de réapprovisionnement : nous vous prévenons dès qu'ils arrivent.", lien);
                    break;
                case StatutCommande.StockReserve when avant == StatutCommande.EnAttenteDisponibilite:
                    Ajouter(c.ClientId, "ProduitsDisponibles", "Vos produits sont arrivés",
                        $"Tous les produits de la commande {c.Reference} sont disponibles : nous la préparons.", lien);
                    break;
                case StatutCommande.PaiementEchoue:
                    var motif = _context.ChangeTracker.Entries<Paiement>()
                        .Where(p => p.Entity.CommandeId == c.Id && p.Entity.Statut == StatutPaiement.Echoue)
                        .Select(p => p.Entity.MotifRejet).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                    Ajouter(c.ClientId, "PaiementRefuse", "Paiement non validé",
                        $"Nous n'avons pas pu valider votre paiement pour la commande {c.Reference}"
                        + (motif is null ? "." : $" : {motif}.") + " Vous pouvez payer à nouveau depuis votre commande.", lien);
                    break;
                case StatutCommande.EnTransit:
                    Ajouter(c.ClientId, "CommandeEnRoute", "Votre commande est en route",
                        $"Le livreur a pris en charge la commande {c.Reference}. Gardez votre téléphone à portée de main.", lien);
                    break;
                case StatutCommande.Livree:
                    Ajouter(c.ClientId, "CommandeLivree", "Commande livrée",
                        $"La commande {c.Reference} a été livrée. Merci pour votre confiance ! Un souci ? Ouvrez une demande SAV depuis la commande.", lien);
                    break;
                case StatutCommande.AnnulationEnCours or StatutCommande.Annulee when avant != StatutCommande.AnnulationEnCours:
                    Ajouter(c.ClientId, "CommandeAnnulee", "Commande annulée",
                        $"La commande {c.Reference} est annulée. Si vous aviez payé, le remboursement ou l'avoir vous sera notifié.", lien);
                    break;
                case StatutCommande.Cloturee when aPayer:
                    Ajouter(c.ClientId, "CommandeExpiree", "Commande annulée",
                        $"La commande {c.Reference} a été annulée faute de paiement dans le délai prévu. Vous pouvez la refaire depuis la boutique.", lien);
                    break;
            }
        }

        private void Devis(EntityEntry e, DevisEntite d)
        {
            if (e.State == EntityState.Modified && Change<StatutDevis>(e, nameof(d.Statut), out _, out var apres) && apres == StatutDevis.Envoye)
                Ajouter(d.ClientId, "DevisEnvoye", "Votre devis est prêt",
                    $"Le devis {d.Reference} de {Fcfa(d.Total)} est disponible, valable jusqu'au {d.DateValidite.ToString("d MMMM yyyy", Fr)}. Acceptez-le en un clic depuis votre espace.",
                    $"/compte/devis/{d.Id}");
        }

        private async Task LivraisonAsync(EntityEntry e, LivraisonEntite l, CancellationToken ct)
        {
            var (clientId, reference) = await CommandeAsync(l.CommandeId, ct);
            var lien = $"/compte/commandes/{l.CommandeId}";
            var objet = l.TicketSAVId.HasValue ? "votre produit (SAV)" : $"la commande {reference}";

            if (Change<DateTime>(e, nameof(l.DatePlanifiee), out _, out _) && l.Statut == StatutLivraison.Planifiee)
                Ajouter(clientId, "LivraisonPlanifiee", "Livraison programmée",
                    $"La livraison de {objet} est prévue le {l.DatePlanifiee.ToString("dddd d MMMM", Fr)}. Le livreur vous appellera avant de passer.", lien);
            else if (e.State == EntityState.Modified && Change<StatutLivraison>(e, nameof(l.Statut), out _, out var apres) && apres == StatutLivraison.Echouee)
                Ajouter(clientId, "LivraisonEchouee", "Livraison non aboutie",
                    $"Nous n'avons pas pu livrer {objet}" + (string.IsNullOrWhiteSpace(l.MotifEchec) ? "." : $" : {l.MotifEchec}.") + " Nous vous recontactons pour convenir d'un nouveau passage.", lien);
        }

        private async Task VersionAsync(EntityEntry e, VersionCommande v, CancellationToken ct)
        {
            if (!Change<StatutVersionCommande>(e, nameof(v.Statut), out _, out var apres) || apres != StatutVersionCommande.EnAttenteClient)
                return;
            var (clientId, reference) = await CommandeAsync(v.CommandeId, ct);
            Ajouter(clientId, "ModificationProposee", "Votre accord est demandé",
                $"Nous proposons une modification de la commande {reference} (nouveau total : {Fcfa(v.Total)}). Acceptez-la ou refusez-la depuis votre commande.",
                $"/compte/commandes/{v.CommandeId}");
        }

        private void TicketSav(EntityEntry e, TicketSAV t)
        {
            if (e.State == EntityState.Modified && Change<StatutSav>(e, nameof(t.Statut), out _, out var apres) && apres == StatutSav.Cloture)
                Ajouter(t.ClientId, "SavCloture", "Demande SAV traitée",
                    $"Votre demande SAV {t.Reference} est terminée" + (string.IsNullOrWhiteSpace(t.Resolution) ? "." : $" : {t.Resolution}."),
                    "/compte/sav");
        }

        private async Task RemboursementAsync(EntityEntry e, Remboursement r, CancellationToken ct)
        {
            if (!Change<StatutRemboursement>(e, nameof(r.Statut), out _, out var apres) || apres != StatutRemboursement.Execute)
                return;
            var (clientId, reference) = await CommandeAsync(r.CommandeId, ct);
            Ajouter(clientId, "RemboursementEffectue", "Remboursement envoyé",
                $"Nous vous avons remboursé {Fcfa(r.Montant)} pour la commande {reference}"
                + (string.IsNullOrWhiteSpace(r.ReferenceTransaction) ? "." : $" (transaction {r.ReferenceTransaction})."),
                $"/compte/commandes/{r.CommandeId}");
        }

        private async Task AvoirAsync(EntityEntry e, Avoir a, CancellationToken ct)
        {
            if (!Change<StatutAvoir>(e, nameof(a.Statut), out _, out var apres) || apres != StatutAvoir.Disponible)
                return;
            var (clientId, _) = await CommandeAsync(a.CommandeId, ct);
            Ajouter(clientId, "AvoirDisponible", "Avoir disponible",
                $"Un avoir {a.Reference} de {Fcfa(a.Montant)} est à votre disposition : il se déduit du paiement de votre prochaine commande.",
                "/compte/avoirs");
        }

        /// <summary>Client et référence d'une commande : d'abord en mémoire, sinon en base.</summary>
        private async Task<(Guid ClientId, string Reference)> CommandeAsync(Guid commandeId, CancellationToken ct)
        {
            if (_commandes.TryGetValue(commandeId, out var connue))
                return connue;

            var suivie = _context.ChangeTracker.Entries<Commande>().FirstOrDefault(c => c.Entity.Id == commandeId)?.Entity;
            var resultat = suivie is not null
                ? (suivie.ClientId, suivie.Reference)
                : await _context.Commandes.AsNoTracking().Where(c => c.Id == commandeId)
                    .Select(c => new ValueTuple<Guid, string>(c.ClientId, c.Reference)).FirstAsync(ct);
            _commandes[commandeId] = resultat;
            return resultat;
        }

        private void Ajouter(Guid clientId, string type, string titre, string message, string? lien)
        {
            // Une même opération ne prévient pas deux fois pour la même chose
            if (_nouvelles.Any(n => n.ClientId == clientId && n.Type == type && n.Lien == lien))
                return;

            _nouvelles.Add(new Notification
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Type = type,
                Titre = titre,
                Message = message.Length > 1000 ? message[..997] + "..." : message,
                Lien = lien
            });
        }

        internal static string Fcfa(decimal montant) =>
            montant.ToString("N0", Fr).Replace(' ', ' ').Replace(' ', ' ') + " FCFA";
    }
}
