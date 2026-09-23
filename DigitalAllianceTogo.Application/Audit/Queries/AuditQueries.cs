using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Domain.Models.Audit;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Audit.Queries
{
    /// <summary>Une ligne du journal : QUI, QUAND, QUOI, AVANT, APRÈS (§32).</summary>
    public class JournalAuditDto
    {
        public Guid Id { get; set; }
        public DateTime DateAction { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entite { get; set; } = string.Empty;
        public Guid EntiteId { get; set; }

        /// <summary>Null = action automatique du système (tâche planifiée).</summary>
        public Guid? UtilisateurId { get; set; }
        public string Auteur { get; set; } = string.Empty;
        public string? AdresseIP { get; set; }

        /// <summary>Instantanés JSON tels qu'enregistrés.</summary>
        public string? Avant { get; set; }
        public string? Apres { get; set; }
    }

    /// <summary>Recherche dans le journal d'audit (Administrateur), le plus récent d'abord.</summary>
    public record GetJournalAuditQuery : IRequest<PaginatedList<JournalAuditDto>>
    {
        public string? Entite { get; init; }
        public Guid? EntiteId { get; init; }
        public string? Action { get; init; }
        public Guid? UtilisateurId { get; init; }

        /// <summary>Vrai : uniquement les actions automatiques (sans utilisateur).</summary>
        public bool? Systeme { get; init; }
        public DateTime? Debut { get; init; }
        public DateTime? Fin { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 50;
    }

    /// <summary>
    /// Tout ce qui est arrivé à une commande, dans l'ordre chronologique : la commande,
    /// son devis d'origine, ses paiements, livraisons, remboursements, avoirs, les mouvements
    /// de stock et les tickets SAV qui s'y rattachent.
    /// </summary>
    public record GetHistoriqueCommandeQuery(Guid CommandeId) : IRequest<List<JournalAuditDto>>;

    public class GetJournalAuditQueryValidator : AbstractValidator<GetJournalAuditQuery>
    {
        public GetJournalAuditQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.Fin).GreaterThan(x => x.Debut).When(x => x.Debut.HasValue && x.Fin.HasValue)
                .WithMessage("La fin de la période doit être après le début.");
            RuleFor(x => x.Entite).MaximumLength(200);
            RuleFor(x => x.Action).MaximumLength(200);
        }
    }

    public class AuditQueriesHandler :
        IRequestHandler<GetJournalAuditQuery, PaginatedList<JournalAuditDto>>,
        IRequestHandler<GetHistoriqueCommandeQuery, List<JournalAuditDto>>
    {
        private readonly IApplicationDbContext _context;

        public AuditQueriesHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<JournalAuditDto>> Handle(GetJournalAuditQuery request, CancellationToken cancellationToken)
        {
            var query = _context.JournauxAudit.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Entite))
                query = query.Where(j => j.Entite == request.Entite.Trim());
            if (request.EntiteId.HasValue)
                query = query.Where(j => j.EntiteId == request.EntiteId.Value);
            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(j => j.Action == request.Action.Trim());
            if (request.UtilisateurId.HasValue)
                query = query.Where(j => j.UtilisateurId == request.UtilisateurId.Value);
            if (request.Systeme == true)
                query = query.Where(j => j.UtilisateurId == null);
            if (request.Debut.HasValue)
            {
                var debut = Utc(request.Debut.Value);
                query = query.Where(j => j.DateAction >= debut);
            }
            if (request.Fin.HasValue)
            {
                var fin = Utc(request.Fin.Value);
                query = query.Where(j => j.DateAction < fin);
            }

            return await PaginatedList<JournalAuditDto>.CreateAsync(
                Projeter(query.OrderByDescending(j => j.DateAction)), request.PageNumber, request.PageSize);
        }

        public async Task<List<JournalAuditDto>> Handle(GetHistoriqueCommandeQuery request, CancellationToken cancellationToken)
        {
            var commande = await _context.Commandes.AsNoTracking()
                .Where(c => c.Id == request.CommandeId)
                .Select(c => new { c.Id, c.DevisOrigineId })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Commande", request.CommandeId);

            // Toutes les entités dont l'historique fait partie de la vie de la commande
            var ids = new List<Guid> { commande.Id };
            if (commande.DevisOrigineId is Guid devisId)
                ids.Add(devisId);
            ids.AddRange(await _context.Paiements.Where(p => p.CommandeId == commande.Id).Select(p => p.Id).ToListAsync(cancellationToken));
            ids.AddRange(await _context.Livraisons.Where(l => l.CommandeId == commande.Id).Select(l => l.Id).ToListAsync(cancellationToken));
            ids.AddRange(await _context.Remboursements.Where(r => r.CommandeId == commande.Id).Select(r => r.Id).ToListAsync(cancellationToken));
            ids.AddRange(await _context.Avoirs.Where(a => a.CommandeId == commande.Id).Select(a => a.Id).ToListAsync(cancellationToken));
            ids.AddRange(await _context.TicketsSAV.Where(t => t.LigneCommande.VersionCommande.CommandeId == commande.Id).Select(t => t.Id).ToListAsync(cancellationToken));

            return await Projeter(_context.JournauxAudit.AsNoTracking()
                    .Where(j => ids.Contains(j.EntiteId))
                    .OrderBy(j => j.DateAction))
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<JournalAuditDto> Projeter(IQueryable<JournalAudit> query) =>
            query.Select(j => new JournalAuditDto
            {
                Id = j.Id,
                DateAction = j.DateAction,
                Action = j.Action,
                Entite = j.Entite,
                EntiteId = j.EntiteId,
                UtilisateurId = j.UtilisateurId,
                Auteur = j.Utilisateur == null ? "Système" : j.Utilisateur.Prenom + " " + j.Utilisateur.Nom,
                AdresseIP = j.AdresseIP,
                Avant = j.AncienneValeur,
                Apres = j.NouvelleValeur
            });

        private static DateTime Utc(DateTime date) => date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }
}
