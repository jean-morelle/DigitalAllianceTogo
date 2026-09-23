using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Audit;
using System.Text.Json;

namespace DigitalAllianceTogo.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AuditService(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public void Enregistrer(string action, string entite, Guid entiteId, object? avant = null, object? apres = null, Guid? auteurId = null)
        {
            // Toute opération auditée doit être rattachée à un utilisateur identifié
            var utilisateurId = auteurId ?? _currentUserService.UtilisateurId
                ?? throw new InvalidOperationException("Impossible de journaliser une opération sans utilisateur authentifié.");

            Ajouter(action, entite, entiteId, avant, apres, utilisateurId);
        }

        public void EnregistrerSysteme(string action, string entite, Guid entiteId, object? avant = null, object? apres = null)
        {
            Ajouter(action, entite, entiteId, avant, apres, utilisateurId: null);
        }

        private void Ajouter(string action, string entite, Guid entiteId, object? avant, object? apres, Guid? utilisateurId)
        {
            _context.JournauxAudit.Add(new JournalAudit
            {
                Id = Guid.NewGuid(),
                DateAction = DateTime.UtcNow,
                Action = action,
                Entite = entite,
                EntiteId = entiteId,
                AncienneValeur = avant is null ? null : JsonSerializer.Serialize(avant),
                NouvelleValeur = apres is null ? null : JsonSerializer.Serialize(apres),
                AdresseIP = utilisateurId is null ? null : _currentUserService.AdresseIP,
                UtilisateurId = utilisateurId
            });
        }
    }
}
