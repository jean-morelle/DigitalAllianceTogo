using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.TableauDeBord.Queries
{
    /// <summary>
    /// Statistiques d'activité sur une période [Debut ; Fin[ (par défaut : les 30 derniers jours).
    /// Les montants sont en FCFA. Les indicateurs « instantané » décrivent la situation actuelle.
    /// </summary>
    public record GetStatistiquesQuery : IRequest<StatistiquesDto>
    {
        public DateTime? Debut { get; init; }
        public DateTime? Fin { get; init; }
    }

    public class GetStatistiquesQueryValidator : AbstractValidator<GetStatistiquesQuery>
    {
        public GetStatistiquesQueryValidator()
        {
            RuleFor(x => x.Fin).GreaterThan(x => x.Debut).When(x => x.Debut.HasValue && x.Fin.HasValue)
                .WithMessage("La fin de la période doit être après le début.");
            RuleFor(x => x).Must(x => !x.Debut.HasValue || !x.Fin.HasValue || (x.Fin.Value - x.Debut.Value).TotalDays <= 366)
                .WithName("Periode").WithMessage("La période ne peut pas dépasser un an.");
        }
    }

    public record StatistiquesDto(
        DateTime Debut,
        DateTime Fin,
        VentesDto Ventes,
        DevisStatsDto Devis,
        List<AcquisitionSourceDto> AcquisitionParSource,
        List<ProduitLivreDto> TopProduitsLivres,
        LivraisonStatsDto Livraisons,
        SavStatsDto Sav,
        InstantaneDto Instantane);

    public record VentesDto(
        int CommandesCreees,
        int PaiementsConfirmes,
        decimal Encaisse,
        decimal Rembourse,
        decimal EncaisseNet,
        decimal PanierMoyen,
        int CommandesLivrees,
        int CommandesAnnulees);

    public record DevisStatsDto(
        int Crees,
        int Acceptes,
        int Refuses,
        int Expires,
        int EnCours,
        decimal TauxAcceptationPourcent,
        decimal RemiseMoyennePourcent,
        int RemisesSoumisesAdmin);

    /// <summary>§37 : les réseaux sociaux amènent-ils de vrais clients payants sur la plateforme ?</summary>
    public record AcquisitionSourceDto(string Source, int NouveauxClients, int ClientsAyantPaye, decimal TauxConversionPourcent, decimal Encaisse);

    /// <summary>Montant = total des lignes (prix × quantité − remise de ligne), avant remise globale du devis.</summary>
    public record ProduitLivreDto(Guid ProduitId, string Reference, string Nom, int Quantite, decimal Montant);

    public record LivraisonStatsDto(int Remises, int Livrees, int LivreesAvecReserve, int AReprogrammer, int RefusClient, decimal TauxReussitePourcent, double? DelaiMoyenPaiementLivraisonHeures);

    public record SavStatsDto(int Ouverts, int Clotures, int Repares, int Remplaces, int Rembourses, int Avoirs, double? DelaiMoyenResolutionJours);

    public record InstantaneDto(
        int CommandesEnAttenteStock,
        int ProduitsSousSeuil,
        int UnitesDefectueuses,
        int UnitesEnTransit,
        int TicketsSavEnCours,
        decimal RemboursementsNonRegles,
        decimal AvoirsDisponibles);

    public class GetStatistiquesQueryHandler : IRequestHandler<GetStatistiquesQuery, StatistiquesDto>
    {
        private readonly IApplicationDbContext _context;

        public GetStatistiquesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StatistiquesDto> Handle(GetStatistiquesQuery request, CancellationToken cancellationToken)
        {
            var fin = request.Fin.HasValue ? Utc(request.Fin.Value) : DateTime.UtcNow;
            var debut = request.Debut.HasValue ? Utc(request.Debut.Value) : fin.AddDays(-30);
            var ct = cancellationToken;

            return new StatistiquesDto(
                debut, fin,
                await VentesAsync(debut, fin, ct),
                await DevisAsync(debut, fin, ct),
                await AcquisitionAsync(debut, fin, ct),
                await TopProduitsAsync(debut, fin, ct),
                await LivraisonsAsync(debut, fin, ct),
                await SavAsync(debut, fin, ct),
                await InstantaneAsync(ct));
        }

        private async Task<VentesDto> VentesAsync(DateTime debut, DateTime fin, CancellationToken ct)
        {
            var paiements = _context.Paiements.Where(p => p.Statut == StatutPaiement.Confirme
                                                          && p.DateConfirmation >= debut && p.DateConfirmation < fin);
            var nbPaiements = await paiements.CountAsync(ct);
            var encaisse = await paiements.SumAsync(p => p.Montant, ct);
            var rembourse = await _context.Remboursements
                .Where(r => r.Statut == StatutRemboursement.Execute && r.DateExecution >= debut && r.DateExecution < fin)
                .SumAsync(r => r.Montant, ct);

            var commandesCreees = await _context.Commandes.CountAsync(c => c.DateCreation >= debut && c.DateCreation < fin, ct);
            var commandesLivrees = await _context.Livraisons
                .Where(l => l.TicketSAVId == null && l.DateLivraison >= debut && l.DateLivraison < fin
                            && (l.Statut == StatutLivraison.Livree || l.Statut == StatutLivraison.LivreeAvecReserve))
                .Select(l => l.CommandeId).Distinct().CountAsync(ct);

            // Pas de date d'annulation sur la commande : le journal d'audit fait foi
            var commandesAnnulees = await _context.JournauxAudit
                .Where(j => j.Entite == "Commande" && (j.Action == "Annulation" || j.Action == "ExpirationCommande")
                            && j.DateAction >= debut && j.DateAction < fin)
                .Select(j => j.EntiteId).Distinct().CountAsync(ct);

            return new VentesDto(commandesCreees, nbPaiements, encaisse, rembourse, encaisse - rembourse,
                nbPaiements == 0 ? 0 : Math.Round(encaisse / nbPaiements, 0), commandesLivrees, commandesAnnulees);
        }

        private async Task<DevisStatsDto> DevisAsync(DateTime debut, DateTime fin, CancellationToken ct)
        {
            // Devis CRÉÉS sur la période, et ce qu'ils sont devenus
            var devis = _context.Devis.Where(d => d.DateCreation >= debut && d.DateCreation < fin);
            var parStatut = await devis.GroupBy(d => d.Statut)
                .Select(g => new { Statut = g.Key, Nombre = g.Count() })
                .ToDictionaryAsync(x => x.Statut, x => x.Nombre, ct);
            int Nb(StatutDevis s) => parStatut.GetValueOrDefault(s);

            var crees = parStatut.Values.Sum();
            var avecMontant = devis.Where(d => d.SousTotal > 0);
            var remiseMoyenne = await avecMontant.AnyAsync(ct)
                ? await avecMontant.AverageAsync(d => d.Remise * 100 / d.SousTotal, ct)
                : 0;

            var soumisAdmin = await _context.JournauxAudit
                .CountAsync(j => j.Action == "SoumissionValidationAdmin" && j.DateAction >= debut && j.DateAction < fin, ct);

            return new DevisStatsDto(
                crees, Nb(StatutDevis.Accepte), Nb(StatutDevis.Refuse), Nb(StatutDevis.Expire),
                Nb(StatutDevis.Brouillon) + Nb(StatutDevis.ValidationInterne) + Nb(StatutDevis.Envoye) + Nb(StatutDevis.ModificationDemandee),
                Pourcent(Nb(StatutDevis.Accepte), crees), Math.Round(remiseMoyenne, 2), soumisAdmin);
        }

        private async Task<List<AcquisitionSourceDto>> AcquisitionAsync(DateTime debut, DateTime fin, CancellationToken ct)
        {
            var nouveaux = await _context.Clients
                .Where(c => c.DateCreation >= debut && c.DateCreation < fin)
                .GroupBy(c => c.Source)
                .Select(g => new
                {
                    Source = g.Key,
                    Nombre = g.Count(),
                    // Converti = au moins un paiement confirmé, à n'importe quelle date
                    AyantPaye = g.Count(c => c.Commandes.Any(cmd => cmd.Paiements.Any(p => p.Statut == StatutPaiement.Confirme)))
                })
                .ToListAsync(ct);

            var encaisseParSource = await _context.Paiements
                .Where(p => p.Statut == StatutPaiement.Confirme && p.DateConfirmation >= debut && p.DateConfirmation < fin)
                .GroupBy(p => p.Commande.Client.Source)
                .Select(g => new { Source = g.Key, Montant = g.Sum(p => p.Montant) })
                .ToDictionaryAsync(x => x.Source, x => x.Montant, ct);

            return System.Enum.GetValues<SourceClient>()
                .Select(source =>
                {
                    var n = nouveaux.FirstOrDefault(x => x.Source == source);
                    return new AcquisitionSourceDto(source.ToString(), n?.Nombre ?? 0, n?.AyantPaye ?? 0,
                        Pourcent(n?.AyantPaye ?? 0, n?.Nombre ?? 0), encaisseParSource.GetValueOrDefault(source));
                })
                .Where(a => a.NouveauxClients > 0 || a.Encaisse > 0)
                .OrderByDescending(a => a.Encaisse).ThenByDescending(a => a.NouveauxClients)
                .ToList();
        }

        private async Task<List<ProduitLivreDto>> TopProduitsAsync(DateTime debut, DateTime fin, CancellationToken ct)
        {
            // « Vendu » = effectivement livré au client sur la période (hors remplacements SAV)
            var commandesLivrees = _context.Livraisons
                .Where(l => l.TicketSAVId == null && l.DateLivraison >= debut && l.DateLivraison < fin
                            && (l.Statut == StatutLivraison.Livree || l.Statut == StatutLivraison.LivreeAvecReserve))
                .Select(l => l.CommandeId);

            var top = await _context.LignesCommande
                .Where(l => commandesLivrees.Contains(l.VersionCommande.CommandeId)
                            && l.VersionCommande.NumeroVersion == l.VersionCommande.Commande.VersionActive)
                .GroupBy(l => new { l.ProduitId, l.Produit.Reference, l.Produit.Nom })
                .Select(g => new { g.Key.ProduitId, g.Key.Reference, g.Key.Nom, Quantite = g.Sum(l => l.Quantite), Montant = g.Sum(l => l.Total) })
                .OrderByDescending(p => p.Quantite).ThenByDescending(p => p.Montant)
                .Take(10)
                .ToListAsync(ct);

            return top.Select(p => new ProduitLivreDto(p.ProduitId, p.Reference, p.Nom, p.Quantite, p.Montant)).ToList();
        }

        private async Task<LivraisonStatsDto> LivraisonsAsync(DateTime debut, DateTime fin, CancellationToken ct)
        {
            // Livraisons parties chez le livreur sur la période, et leur issue
            var parStatut = await _context.Livraisons
                .Where(l => l.DatePriseEnCharge >= debut && l.DatePriseEnCharge < fin)
                .GroupBy(l => l.Statut)
                .Select(g => new { Statut = g.Key, Nombre = g.Count() })
                .ToDictionaryAsync(x => x.Statut, x => x.Nombre, ct);
            int Nb(StatutLivraison s) => parStatut.GetValueOrDefault(s);

            var reussies = Nb(StatutLivraison.Livree) + Nb(StatutLivraison.LivreeAvecReserve);
            var echouees = Nb(StatutLivraison.AReprogrammer) + Nb(StatutLivraison.Echouee) + Nb(StatutLivraison.Retournee);

            // Délai entre la confirmation du paiement et la livraison (commandes livrées sur la période)
            var delais = await _context.Livraisons
                .Where(l => l.TicketSAVId == null && l.DateLivraison >= debut && l.DateLivraison < fin
                            && (l.Statut == StatutLivraison.Livree || l.Statut == StatutLivraison.LivreeAvecReserve))
                .Select(l => new
                {
                    l.DateLivraison,
                    Paiement = l.Commande.Paiements.Where(p => p.Statut == StatutPaiement.Confirme).Min(p => p.DateConfirmation)
                })
                .ToListAsync(ct);
            var heures = delais.Where(d => d.Paiement.HasValue && d.DateLivraison.HasValue)
                .Select(d => (d.DateLivraison!.Value - d.Paiement!.Value).TotalHours)
                .ToList();

            return new LivraisonStatsDto(parStatut.Values.Sum(), Nb(StatutLivraison.Livree), Nb(StatutLivraison.LivreeAvecReserve),
                Nb(StatutLivraison.AReprogrammer), Nb(StatutLivraison.Echouee) + Nb(StatutLivraison.Retournee),
                Pourcent(reussies, reussies + echouees), heures.Count == 0 ? null : Math.Round(heures.Average(), 1));
        }

        private async Task<SavStatsDto> SavAsync(DateTime debut, DateTime fin, CancellationToken ct)
        {
            var ouverts = await _context.TicketsSAV.CountAsync(t => t.DateCreation >= debut && t.DateCreation < fin, ct);

            var clotures = await _context.TicketsSAV
                .Where(t => t.Statut == StatutSav.Cloture && t.DateCloture >= debut && t.DateCloture < fin)
                .Select(t => new { t.Decision, t.DateCreation, t.DateCloture })
                .ToListAsync(ct);

            var jours = clotures.Select(t => (t.DateCloture!.Value - t.DateCreation).TotalDays).ToList();
            return new SavStatsDto(
                ouverts,
                clotures.Count,
                clotures.Count(t => t.Decision == null),
                clotures.Count(t => t.Decision == DecisionSav.Remplacement),
                clotures.Count(t => t.Decision == DecisionSav.Remboursement),
                clotures.Count(t => t.Decision == DecisionSav.Avoir),
                jours.Count == 0 ? null : Math.Round(jours.Average(), 1));
        }

        private async Task<InstantaneDto> InstantaneAsync(CancellationToken ct)
        {
            var stocks = _context.StocksProduit;
            return new InstantaneDto(
                await _context.Commandes.CountAsync(c => c.Statut == StatutCommande.EnAttenteDisponibilite, ct),
                await stocks.CountAsync(s => s.SeuilAlerte > 0 && s.QuantitePhysique - s.QuantiteReservee <= s.SeuilAlerte, ct),
                await stocks.SumAsync(s => s.QuantiteDefectueuse, ct),
                await stocks.SumAsync(s => s.QuantiteEnTransit, ct),
                await _context.TicketsSAV.CountAsync(t => t.Statut != StatutSav.Cloture, ct),
                await _context.Remboursements
                    .Where(r => r.Statut == StatutRemboursement.EnAttente || r.Statut == StatutRemboursement.Valide || r.Statut == StatutRemboursement.Echoue)
                    .SumAsync(r => r.Montant, ct),
                await _context.Avoirs.Where(a => a.Statut == StatutAvoir.Disponible).SumAsync(a => a.Montant, ct));
        }

        private static decimal Pourcent(int partie, int total) => total == 0 ? 0 : Math.Round(partie * 100m / total, 1);

        private static DateTime Utc(DateTime date) => date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }
}
