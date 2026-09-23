using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Stock.Commands.EntreeStock;
using DigitalAllianceTogo.Application.Stock.Commands.SurplusFournisseur;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Stock
{
    /// <summary>Surplus fournisseur (§30) : jamais intégré au stock sans décision.</summary>
    public class SurplusFournisseurTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new() { UtilisateurId = Guid.NewGuid(), Roles = new[] { Roles.GestionnaireStock } };
        private readonly AuditService _audit;
        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _entrepotId = Guid.NewGuid();

        public SurplusFournisseurTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
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
        public async Task Seule_la_quantite_commandee_entre_le_surplus_attend_une_decision()
        {
            var result = await RecevoirAsync(recue: 50, commandee: 40);

            Assert.Equal(40, result.QuantitePhysique);
            Assert.Equal(10, result.SurplusEnAttente);
            var ecart = await _context.EcartsReception.SingleAsync();
            Assert.Equal(StatutEcartReception.EnAttenteDecision, ecart.Statut);
            Assert.Equal(10, ecart.Surplus);
            Assert.Equal("BL-001", ecart.Reference);
        }

        [Theory]
        [InlineData(null)] // quantité commandée inconnue : tout entre
        [InlineData(40)]   // livraison incomplète : ce qui est reçu entre
        public async Task Sans_surplus_tout_ce_qui_est_recu_entre(int? commandee)
        {
            var result = await RecevoirAsync(recue: 30, commandee: commandee);

            Assert.Equal(30, result.QuantitePhysique);
            Assert.Null(result.EcartId);
            Assert.False(await _context.EcartsReception.AnyAsync());
        }

        [Fact]
        public async Task Integration_du_surplus_validee_par_l_admin_une_seule_fois()
        {
            var result = await RecevoirAsync(recue: 50, commandee: 40);
            var handler = new EcartReceptionHandler(_context, _user, _audit);

            _user.Roles = new[] { Roles.Admin };
            await handler.Handle(new DeciderEcartReceptionCommand
            {
                Id = result.EcartId!.Value, Decision = DecisionEcartReception.IntegrerAuStock, Motif = "Geste commercial du fournisseur"
            }, default);

            Assert.Equal(50, (await _context.StocksProduit.SingleAsync()).QuantitePhysique);
            var ecart = await _context.EcartsReception.SingleAsync();
            Assert.Equal(StatutEcartReception.IntegreAuStock, ecart.Statut);
            Assert.Equal(_user.UtilisateurId, ecart.DecideParId);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new DeciderEcartReceptionCommand
            {
                Id = ecart.Id, Decision = DecisionEcartReception.IntegrerAuStock, Motif = "Doublon"
            }, default));
            Assert.Equal(50, (await _context.StocksProduit.SingleAsync()).QuantitePhysique);
        }

        [Fact]
        public async Task Retour_fournisseur_le_surplus_n_entre_jamais()
        {
            var result = await RecevoirAsync(recue: 50, commandee: 40);

            _user.Roles = new[] { Roles.Admin };
            await new EcartReceptionHandler(_context, _user, _audit).Handle(new DeciderEcartReceptionCommand
            {
                Id = result.EcartId!.Value, Decision = DecisionEcartReception.RetournerAuFournisseur, Motif = "Non commandé, repris par le fournisseur"
            }, default);

            Assert.Equal(40, (await _context.StocksProduit.SingleAsync()).QuantitePhysique);
            Assert.Equal(StatutEcartReception.RetourneFournisseur, (await _context.EcartsReception.SingleAsync()).Statut);
        }

        private Task<EntreeStockResult> RecevoirAsync(int recue, int? commandee) =>
            new EntreeStockCommandHandler(_context, _user, _audit).Handle(new EntreeStockCommand
            {
                ProduitId = _produitId, EntrepotId = _entrepotId, Quantite = recue, QuantiteCommandee = commandee, Reference = "BL-001"
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
