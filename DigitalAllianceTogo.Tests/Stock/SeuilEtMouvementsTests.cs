using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Stock.Commands.EntreeStock;
using DigitalAllianceTogo.Application.Stock.Commands.SeuilAlerte;
using DigitalAllianceTogo.Application.Stock.Queries.GetMouvements;
using DigitalAllianceTogo.Application.Stock.Queries.GetStocks;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Stock
{
    /// <summary>Seuil d'alerte et historique des mouvements (§28).</summary>
    public class SeuilEtMouvementsTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new() { UtilisateurId = Guid.NewGuid(), Roles = new[] { Roles.GestionnaireStock } };
        private readonly AuditService _audit;
        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _entrepotId = Guid.NewGuid();

        public SeuilEtMouvementsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _audit = new AuditService(_context, _user);

            var categorie = new Categorie { Id = Guid.NewGuid(), Nom = "Informatique" };
            var marque = new Marque { Id = Guid.NewGuid(), Nom = "HP" };
            _context.Categories.Add(categorie);
            _context.Marques.Add(marque);
            _context.Produits.Add(new Produit { Id = _produitId, Reference = "HP-250", Nom = "HP 250 G9", Prix = 300_000m, CategorieId = categorie.Id, MarqueId = marque.Id });
            _context.Entrepots.Add(new Entrepot { Id = _entrepotId, Nom = "Lomé", Adresse = "Lomé" });
            _context.SaveChanges();
        }

        [Fact]
        public async Task Le_seuil_d_alerte_fait_apparaitre_le_produit_sous_le_seuil()
        {
            await RecevoirAsync(3);
            var stockId = (await _context.StocksProduit.SingleAsync()).Id;
            var stocks = new GetStocksQueryHandler(_context);

            Assert.Empty(await stocks.Handle(new GetStocksQuery { SousSeuilSeulement = true }, default)); // seuil 0 = pas d'alerte

            await new ModifierSeuilAlerteCommandHandler(_context, _audit).Handle(new ModifierSeuilAlerteCommand { StockProduitId = stockId, SeuilAlerte = 5 }, default);

            var sousSeuil = Assert.Single(await stocks.Handle(new GetStocksQuery { SousSeuilSeulement = true }, default));
            Assert.Equal(5, sousSeuil.SeuilAlerte);
            Assert.True(await _context.JournauxAudit.AnyAsync(j => j.Action == "ModificationSeuilAlerte" && j.EntiteId == stockId));
        }

        [Fact]
        public async Task Seuil_d_un_stock_inconnu()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => new ModifierSeuilAlerteCommandHandler(_context, _audit)
                .Handle(new ModifierSeuilAlerteCommand { StockProduitId = Guid.NewGuid(), SeuilAlerte = 2 }, default));
        }

        [Fact]
        public async Task Historique_des_mouvements_du_plus_recent_au_plus_ancien()
        {
            await RecevoirAsync(3, "BL-001");
            await RecevoirAsync(2, "BL-002");
            var stockId = (await _context.StocksProduit.SingleAsync()).Id;

            var mouvements = await new GetMouvementsStockQueryHandler(_context).Handle(new GetMouvementsStockQuery { StockProduitId = stockId }, default);

            Assert.Equal(new[] { "BL-002", "BL-001" }, mouvements.Items.Select(m => m.Reference));
            Assert.All(mouvements.Items, m => Assert.Equal("Entree", m.Type));
        }

        private Task<EntreeStockResult> RecevoirAsync(int quantite, string reference = "BL-000") =>
            new EntreeStockCommandHandler(_context, _user, _audit).Handle(new EntreeStockCommand
            {
                ProduitId = _produitId, EntrepotId = _entrepotId, Quantite = quantite, Reference = reference
            }, default);

        private sealed class FakeCurrentUser : ICurrentUserService
        {
            public Guid? UtilisateurId { get; set; }
            public string[] Roles { get; set; } = Array.Empty<string>();
            public bool EstAuthentifie => UtilisateurId.HasValue;
            public string? AdresseIP => "127.0.0.1";
            public bool EstDansRole(string role) => Roles.Contains(role);
        }
    }
}
