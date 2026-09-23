using System.Net;
using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Domain.Models.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Notifications
{
    public record NotificationDto(Guid Id, DateTime DateCreation, string Type, string Titre, string Message, string? Lien, bool Lue);

    /// <summary>Notifications du client connecté, les plus récentes d'abord.</summary>
    public record GetMesNotificationsQuery : IRequest<PaginatedList<NotificationDto>>
    {
        public bool NonLuesSeulement { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record GetNombreNonLuesQuery : IRequest<int>;

    /// <summary>Marque une notification lue (ou toutes si Id est null).</summary>
    public record MarquerLuesCommand(Guid? Id) : IRequest;

    public class MesNotificationsHandler :
        IRequestHandler<GetMesNotificationsQuery, PaginatedList<NotificationDto>>,
        IRequestHandler<GetNombreNonLuesQuery, int>,
        IRequestHandler<MarquerLuesCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _user;

        public MesNotificationsHandler(IApplicationDbContext context, ICurrentUserService user)
        {
            _context = context;
            _user = user;
        }

        public async Task<PaginatedList<NotificationDto>> Handle(GetMesNotificationsQuery request, CancellationToken cancellationToken)
        {
            var clientId = await ClientHelper.IdClientConnecteAsync(_context, _user, cancellationToken);
            var query = _context.Notifications.AsNoTracking().Where(n => n.ClientId == clientId);
            if (request.NonLuesSeulement)
                query = query.Where(n => n.DateLecture == null);

            var dtos = query.OrderByDescending(n => n.DateCreation)
                .Select(n => new NotificationDto(n.Id, n.DateCreation, n.Type, n.Titre, n.Message, n.Lien, n.DateLecture != null));
            return await PaginatedList<NotificationDto>.CreateAsync(dtos, request.PageNumber, Math.Clamp(request.PageSize, 1, 50));
        }

        public async Task<int> Handle(GetNombreNonLuesQuery request, CancellationToken cancellationToken)
        {
            var clientId = await ClientHelper.IdClientConnecteAsync(_context, _user, cancellationToken);
            return await _context.Notifications.CountAsync(n => n.ClientId == clientId && n.DateLecture == null, cancellationToken);
        }

        public async Task Handle(MarquerLuesCommand request, CancellationToken cancellationToken)
        {
            var clientId = await ClientHelper.IdClientConnecteAsync(_context, _user, cancellationToken);
            var query = _context.Notifications.Where(n => n.ClientId == clientId && n.DateLecture == null);
            if (request.Id.HasValue)
            {
                // Une notification d'un autre client : introuvable, sans dire qu'elle existe
                if (!await _context.Notifications.AnyAsync(n => n.Id == request.Id && n.ClientId == clientId, cancellationToken))
                    throw new NotFoundException("Notification", request.Id.Value);
                query = query.Where(n => n.Id == request.Id);
            }

            var maintenant = DateTime.UtcNow;
            foreach (var n in await query.ToListAsync(cancellationToken))
                n.DateLecture = maintenant;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Envoie par e-mail les notifications en attente (tâche de fond). Sans adresse, ou sans
    /// serveur d'envoi configuré, la notification reste visible dans l'espace client seulement.
    /// Échec d'envoi : nouvel essai au passage suivant, abandon après <see cref="TentativesMax"/>.
    /// </summary>
    public record EnvoyerNotificationsEmailCommand : IRequest<int>;

    public class EnvoyerNotificationsEmailCommandHandler : IRequestHandler<EnvoyerNotificationsEmailCommand, int>
    {
        public const int TentativesMax = 5;
        private const int TailleLot = 20;

        private readonly IApplicationDbContext _context;
        private readonly IEnvoiEmail _email;

        public EnvoyerNotificationsEmailCommandHandler(IApplicationDbContext context, IEnvoiEmail email)
        {
            _context = context;
            _email = email;
        }

        public async Task<int> Handle(EnvoyerNotificationsEmailCommand request, CancellationToken cancellationToken)
        {
            var lot = await _context.Notifications
                .Include(n => n.Client).ThenInclude(c => c.Utilisateur)
                .Where(n => n.StatutEmail == StatutEnvoiNotification.EnAttente)
                .OrderBy(n => n.DateCreation)
                .Take(TailleLot)
                .ToListAsync(cancellationToken);

            var envoyees = 0;
            foreach (var n in lot)
            {
                var adresse = n.Client.Email ?? n.Client.Utilisateur?.Email;
                if (!_email.EstConfigure || string.IsNullOrWhiteSpace(adresse))
                {
                    n.StatutEmail = StatutEnvoiNotification.NonEnvoye;
                    continue;
                }

                try
                {
                    await _email.EnvoyerAsync(adresse, n.Titre, Corps(n), cancellationToken);
                    n.StatutEmail = StatutEnvoiNotification.Envoye;
                    n.DateEnvoiEmail = DateTime.UtcNow;
                    n.ErreurEmail = null;
                    envoyees++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    n.TentativesEmail++;
                    n.ErreurEmail = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    if (n.TentativesEmail >= TentativesMax)
                        n.StatutEmail = StatutEnvoiNotification.Echec;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return envoyees;
        }

        private string Corps(Notification n)
        {
            var bouton = n.Lien is null ? "" :
                $"""<p><a href="{WebUtility.HtmlEncode(_email.LienSite(n.Lien))}" style="background:#1d4ed8;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none;display:inline-block">Voir dans mon espace</a></p>""";
            return $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;color:#111">
                  <h2 style="font-size:18px">{WebUtility.HtmlEncode(n.Titre)}</h2>
                  <p style="line-height:1.5">{WebUtility.HtmlEncode(n.Message)}</p>
                  {bouton}
                  <p style="color:#666;font-size:12px;margin-top:24px">Togo Informatique</p>
                </div>
                """;
        }
    }
}
