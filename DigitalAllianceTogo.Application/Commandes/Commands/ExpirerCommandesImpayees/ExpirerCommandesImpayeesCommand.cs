using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.ExpirerCommandesImpayees
{
    /// <summary>
    /// Tâche SYSTÈME (planifiée) : annule les commandes jamais payées, ou dont le paiement
    /// a échoué, une fois le délai de nouvelle tentative dépassé (§9).
    /// Les commandes dont la preuve attend la vérification du Commercial ne sont
    /// jamais annulées : l'attente vient alors de l'entreprise, pas du client.
    /// Retourne le nombre de commandes annulées.
    /// </summary>
    public record ExpirerCommandesImpayeesCommand : IRequest<int>;

    public class ExpirerCommandesImpayeesCommandHandler : IRequestHandler<ExpirerCommandesImpayeesCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ExpirerCommandesImpayeesCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<int> Handle(ExpirerCommandesImpayeesCommand request, CancellationToken cancellationToken)
        {
            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            var maintenant = DateTime.UtcNow;
            // Le délai court au plus tôt depuis la création : filtre grossier côté base
            var limite = maintenant.AddHours(-parametres.DelaiExpirationPaiementHeures);

            var candidates = await _context.Commandes
                .Include(c => c.Paiements)
                .Where(c => (c.Statut == StatutCommande.CommandeCreee || c.Statut == StatutCommande.PaiementEchoue)
                            && c.DateCreation <= limite)
                .ToListAsync(cancellationToken);

            var annulees = 0;
            foreach (var commande in candidates)
            {
                var avant = commande.Statut.ToString();
                if (!CommandeHelper.AnnulerSiDelaiDepasse(commande, parametres.DelaiExpirationPaiementHeures, maintenant))
                    continue;

                _audit.EnregistrerSysteme("ExpirationCommande", "Commande", commande.Id,
                    new { Statut = avant },
                    new { Statut = commande.Statut.ToString(), Motif = $"Impayée après {parametres.DelaiExpirationPaiementHeures} h" });
                annulees++;
            }

            if (annulees > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return annulees;
        }
    }
}
