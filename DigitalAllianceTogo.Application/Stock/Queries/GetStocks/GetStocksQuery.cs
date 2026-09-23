using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Queries.GetStocks
{
    /// <summary>État du stock par produit et par entrepôt (disponible = physique − réservé).</summary>
    public record GetStocksQuery : IRequest<List<StockProduitDto>>
    {
        public Guid? ProduitId { get; init; }
        public Guid? EntrepotId { get; init; }
        public bool SousSeuilSeulement { get; init; }
    }

    public class StockProduitDto
    {
        public Guid Id { get; set; }
        public Guid ProduitId { get; set; }
        public string ProduitReference { get; set; } = string.Empty;
        public string ProduitNom { get; set; } = string.Empty;
        public Guid EntrepotId { get; set; }
        public string EntrepotNom { get; set; } = string.Empty;
        public int QuantitePhysique { get; set; }
        public int QuantiteReservee { get; set; }
        public int QuantiteDisponible { get; set; }
        public int QuantiteEnTransit { get; set; }
        public int QuantiteDefectueuse { get; set; }
        public int SeuilAlerte { get; set; }
    }

    public record EntrepotDto(Guid Id, string Nom, string Adresse, bool Actif);

    public record GetEntrepotsQuery : IRequest<List<EntrepotDto>>;

    public class GetStocksQueryHandler : IRequestHandler<GetStocksQuery, List<StockProduitDto>>, IRequestHandler<GetEntrepotsQuery, List<EntrepotDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetStocksQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockProduitDto>> Handle(GetStocksQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StocksProduit.AsNoTracking().AsQueryable();

            if (request.ProduitId.HasValue)
                query = query.Where(s => s.ProduitId == request.ProduitId.Value);
            if (request.EntrepotId.HasValue)
                query = query.Where(s => s.EntrepotId == request.EntrepotId.Value);
            // QuantiteDisponible n'est pas en base : calcul explicite pour que le filtre reste en SQL
            if (request.SousSeuilSeulement)
                query = query.Where(s => s.QuantitePhysique - s.QuantiteReservee <= s.SeuilAlerte);

            return await query
                .OrderBy(s => s.Produit.Nom).ThenBy(s => s.Entrepot.Nom)
                .Select(s => new StockProduitDto
                {
                    Id = s.Id,
                    ProduitId = s.ProduitId,
                    ProduitReference = s.Produit.Reference,
                    ProduitNom = s.Produit.Nom,
                    EntrepotId = s.EntrepotId,
                    EntrepotNom = s.Entrepot.Nom,
                    QuantitePhysique = s.QuantitePhysique,
                    QuantiteReservee = s.QuantiteReservee,
                    QuantiteDisponible = s.QuantitePhysique - s.QuantiteReservee,
                    QuantiteEnTransit = s.QuantiteEnTransit,
                    QuantiteDefectueuse = s.QuantiteDefectueuse,
                    SeuilAlerte = s.SeuilAlerte
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<EntrepotDto>> Handle(GetEntrepotsQuery request, CancellationToken cancellationToken)
        {
            return await _context.Entrepots.AsNoTracking()
                .OrderBy(e => e.Nom)
                .Select(e => new EntrepotDto(e.Id, e.Nom, e.Adresse, e.Actif))
                .ToListAsync(cancellationToken);
        }
    }
}
