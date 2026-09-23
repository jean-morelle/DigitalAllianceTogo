using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.SAV;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Sav.Common
{
    internal static class SavHelper
    {
        /// <summary>Le personnel voit tous les tickets ; un client seulement les siens.</summary>
        public static bool VoitTousLesTickets(ICurrentUserService currentUser) =>
            currentUser.EstDansRole(Roles.Admin) || currentUser.EstDansRole(Roles.Commercial)
            || currentUser.EstDansRole(Roles.Technicien) || currentUser.EstDansRole(Roles.GestionnaireStock);

        public static async Task<TicketSAV> ChargerAvecControleAccesAsync(
            IApplicationDbContext context, ICurrentUserService currentUser, Guid ticketId, CancellationToken cancellationToken)
        {
            var ticket = await context.TicketsSAV
                .Include(t => t.Client)
                .Include(t => t.LigneCommande)
                .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
                ?? throw new NotFoundException("TicketSAV", ticketId);

            if (!VoitTousLesTickets(currentUser) && ticket.Client.UtilisateurId != currentUser.UtilisateurId)
                throw new ForbiddenAccessException();

            return ticket;
        }

        public static void ExigerStatut(TicketSAV ticket, params StatutSav[] attendus)
        {
            if (!attendus.Contains(ticket.Statut))
                throw new ConflictException($"Le ticket est au statut {ticket.Statut} (attendu : {string.Join(" ou ", attendus)}).");
        }

        public static void ChangerStatut(IAuditService audit, TicketSAV ticket, StatutSav nouveau, string action, object? details = null)
        {
            var avant = ticket.Statut.ToString();
            ticket.Statut = nouveau;
            audit.Enregistrer(action, "TicketSAV", ticket.Id, new { Statut = avant }, new { Statut = nouveau.ToString(), Details = details });
        }

        public static void Cloturer(IAuditService audit, TicketSAV ticket, string resolution)
        {
            ticket.Resolution = resolution;
            ticket.DateCloture = DateTime.UtcNow;
            ChangerStatut(audit, ticket, StatutSav.Cloture, "ClotureTicketSav", new { Resolution = resolution });
        }

        /// <summary>Après un remboursement exécuté ou un avoir traité : clôt le ticket qui n'attendait plus que ça.</summary>
        public static async Task CloturerSiRegulariseAsync(IApplicationDbContext context, IAuditService audit, Guid ticketId, CancellationToken cancellationToken)
        {
            var ticket = await context.TicketsSAV.FirstAsync(t => t.Id == ticketId, cancellationToken);
            if (ticket.Statut == StatutSav.EnAttenteRegulationFinanciere
                && !await RegularisationFinanciere.EnCoursPourTicketAsync(context, ticketId, cancellationToken))
            {
                var resolution = ticket.Decision != DecisionSav.Avoir
                    ? "Remboursé"
                    : context.Avoirs.Local.Any(a => a.TicketSAVId == ticketId && a.Statut == StatutAvoir.Disponible)
                        ? "Avoir accordé"
                        : "Avoir annulé par l'Administrateur";
                Cloturer(audit, ticket, resolution);
            }
        }
    }
}
