using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.TableauDeBord.Queries
{
    /// <summary>
    /// Une file de travail : combien d'éléments attendent, depuis quand (le plus ancien),
    /// et quel rôle doit agir.
    /// </summary>
    public record FileDeTravailDto(string Cle, string Libelle, string Responsable, int Nombre, DateTime? PlusAncien);

    /// <summary>
    /// Supervision (§42, phase 7) : tout ce qui attend une action humaine.
    /// Chaque utilisateur ne reçoit que les files de ses rôles ; l'Administrateur voit tout.
    /// </summary>
    public record GetATraiterQuery : IRequest<List<FileDeTravailDto>>;

    public class GetATraiterQueryHandler : IRequestHandler<GetATraiterQuery, List<FileDeTravailDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetATraiterQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<List<FileDeTravailDto>> Handle(GetATraiterQuery request, CancellationToken cancellationToken)
        {
            var files = new List<FileDeTravailDto>();
            var ct = cancellationToken;

            if (Voit(Roles.Commercial))
            {
                files.Add(await FileAsync("paiements-a-verifier", "Preuves de paiement à vérifier", Roles.Commercial,
                    _context.Paiements.Where(p => p.Statut == StatutPaiement.EnAttente).Select(p => p.DatePaiement), ct));
                files.Add(await FileAsync("devis-a-envoyer", "Devis validés pas encore envoyés", Roles.Commercial,
                    _context.Devis.Where(d => d.Statut == StatutDevis.Brouillon && d.ValideParId != null).Select(d => d.DateCreation), ct));
                files.Add(await FileAsync("devis-modification-demandee", "Devis dont le client demande une modification", Roles.Commercial,
                    _context.Devis.Where(d => d.Statut == StatutDevis.ModificationDemandee).Select(d => d.DateCreation), ct));
                files.Add(await FileAsync("sav-decision-commerciale", "Tickets SAV irréparables : choix du client à enregistrer", Roles.Commercial,
                    _context.TicketsSAV.Where(t => t.Statut == StatutSav.DecisionCommerciale).Select(t => t.DateCreation), ct));
            }

            if (Voit(Roles.Admin))
            {
                files.Add(await FileAsync("devis-remise-exceptionnelle", "Remises exceptionnelles à valider", Roles.Admin,
                    _context.Devis.Where(d => d.Statut == StatutDevis.ValidationInterne).Select(d => d.DateCreation), ct));
                files.Add(await FileAsync("remboursements-a-valider", "Remboursements à valider", Roles.Admin,
                    _context.Remboursements.Where(r => r.Statut == StatutRemboursement.EnAttente).Select(r => r.DateDemande), ct));
                files.Add(await FileAsync("remboursements-a-executer", "Remboursements validés à exécuter", Roles.Admin,
                    _context.Remboursements.Where(r => r.Statut == StatutRemboursement.Valide).Select(r => r.DateDemande), ct));
                files.Add(await FileAsync("remboursements-echoues", "Remboursements échoués à régulariser", Roles.Admin,
                    _context.Remboursements.Where(r => r.Statut == StatutRemboursement.Echoue).Select(r => r.DateDemande), ct));
                files.Add(await FileAsync("avoirs-a-valider", "Avoirs à valider", Roles.Admin,
                    _context.Avoirs.Where(a => a.Statut == StatutAvoir.EnAttente).Select(a => a.DateCreation), ct));
            }

            if (Voit(Roles.GestionnaireStock))
            {
                files.Add(await FileAsync("commandes-a-preparer", "Commandes payées, stock réservé : à préparer", Roles.GestionnaireStock,
                    _context.Commandes.Where(c => c.Statut == StatutCommande.StockReserve).Select(c => c.DateCreation), ct));
                files.Add(await FileAsync("commandes-a-livrer", "Commandes prêtes sans livraison planifiée", Roles.GestionnaireStock,
                    _context.Commandes.Where(c => c.Statut == StatutCommande.PretePourLivraison
                            && !c.Livraisons.Any(l => l.Statut == StatutLivraison.Planifiee || l.Statut == StatutLivraison.EnTransit))
                        .Select(c => c.DateCreation), ct));
                files.Add(await FileAsync("retours-a-controler", "Retours à réceptionner et contrôler", Roles.GestionnaireStock,
                    _context.Commandes.Where(c => c.Statut == StatutCommande.AnnulationEnCours || c.Statut == StatutCommande.LivraisonEchoueeRefusClient)
                        .Select(c => c.DateCreation), ct));
                files.Add(await FileAsync("commandes-en-rupture", "Commandes payées en attente de stock", Roles.GestionnaireStock,
                    _context.Commandes.Where(c => c.Statut == StatutCommande.EnAttenteDisponibilite).Select(c => c.DateCreation), ct));
                files.Add(await FileAsync("remplacements-sav-a-livrer", "Remplacements SAV sans livraison planifiée", Roles.GestionnaireStock,
                    _context.TicketsSAV.Where(t => t.Statut == StatutSav.RemplacementEnCours
                            && !_context.Livraisons.Any(l => l.TicketSAVId == t.Id && (l.Statut == StatutLivraison.Planifiee || l.Statut == StatutLivraison.EnTransit)))
                        .Select(t => t.DateCreation), ct));
                // Seuil 0 = pas d'alerte configurée pour ce produit
                files.Add(await FileAsync("stocks-sous-seuil", "Produits sous le seuil d'alerte", Roles.GestionnaireStock,
                    _context.StocksProduit.Where(s => s.SeuilAlerte > 0 && s.QuantitePhysique - s.QuantiteReservee <= s.SeuilAlerte)
                        .Select(s => (DateTime?)null), ct));
            }

            if (Voit(Roles.Technicien))
            {
                files.Add(await FileAsync("sav-a-diagnostiquer", "Tickets SAV à diagnostiquer", Roles.Technicien,
                    _context.TicketsSAV.Where(t => t.Statut == StatutSav.Ouvert).Select(t => t.DateCreation), ct));
                files.Add(await FileAsync("sav-en-reparation", "Réparations en cours", Roles.Technicien,
                    _context.TicketsSAV.Where(t => t.Statut == StatutSav.EnReparation).Select(t => t.DateCreation), ct));
            }

            return files;
        }

        private bool Voit(string role) => _currentUser.EstDansRole(Roles.Admin) || _currentUser.EstDansRole(role);

        private static async Task<FileDeTravailDto> FileAsync(
            string cle, string libelle, string responsable, IQueryable<DateTime> dates, CancellationToken cancellationToken)
        {
            var nombre = await dates.CountAsync(cancellationToken);
            var plusAncien = nombre == 0 ? (DateTime?)null : await dates.MinAsync(cancellationToken);
            return new FileDeTravailDto(cle, libelle, responsable, nombre, plusAncien);
        }

        private static async Task<FileDeTravailDto> FileAsync(
            string cle, string libelle, string responsable, IQueryable<DateTime?> dates, CancellationToken cancellationToken)
        {
            // File sans notion d'ancienneté (ex : niveau de stock)
            var nombre = await dates.CountAsync(cancellationToken);
            return new FileDeTravailDto(cle, libelle, responsable, nombre, null);
        }
    }
}
