using DigitalAllianceTogo.Application.Catalogue;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Produits.Commands.AjouterImage;
using DigitalAllianceTogo.Application.Produits.Commands.ModifierProduit;
using DigitalAllianceTogo.Application.Produits.Commands.SupprimerAttribut; // contient aussi SupprimerImageCommand
using DigitalAllianceTogo.Application.Produits.Commands.SupprimerProduit;
using DigitalAllianceTogo.Application.Produits.Queries.GetProduits;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Catalogue
{
    /// <summary>Gestion du catalogue : visibilité publique, images, suppression, référentiels.</summary>
    public class CatalogueTests
    {
        private const string Photo = "/api/fichiers/produits/0123456789abcdef0123456789abcdef.jpg";

        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new() { UtilisateurId = Guid.NewGuid() };
        private readonly AuditService _audit;
        private readonly Categorie _categorie = new() { Id = Guid.NewGuid(), Nom = "Ordinateurs" };
        private readonly Marque _marque = new() { Id = Guid.NewGuid(), Nom = "HP" };
        private readonly Produit _pc;
        private readonly Produit _ancien;

        public CatalogueTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _audit = new AuditService(_context, _user);

            _pc = new Produit { Id = Guid.NewGuid(), Reference = "HP-250", Nom = "HP 250 G9", Prix = 300_000m, CategorieId = _categorie.Id, MarqueId = _marque.Id };
            _ancien = new Produit { Id = Guid.NewGuid(), Reference = "HP-OLD", Nom = "HP ancien", Prix = 100_000m, Actif = false, CategorieId = _categorie.Id, MarqueId = _marque.Id };
            _context.AddRange(_categorie, _marque, _pc, _ancien);
            _context.SaveChanges();
        }

        private Task<List<string>> NomsVisiblesAsync() =>
            new GetProduitsQueryHandler(_context, _user).Handle(new GetProduitsQuery(), default)
                .ContinueWith(t => t.Result.Items.Select(p => p.Nom).ToList());

        [Fact]
        public async Task Le_public_ne_voit_que_les_produits_en_vente()
        {
            Assert.Equal(new[] { "HP 250 G9" }, await NomsVisiblesAsync());

            _user.Roles = new[] { Roles.Catalogue };
            Assert.Equal(2, (await NomsVisiblesAsync()).Count);
        }

        [Fact]
        public async Task Retirer_une_marque_masque_ses_produits()
        {
            await new CatalogueHandler(_context, _audit).Handle(
                new ModifierReferentielCommand(TypeReferentiel.Marque, _marque.Id, "HP", "", Actif: false), default);

            Assert.Empty(await NomsVisiblesAsync());
            Assert.True(await _context.JournauxAudit.AnyAsync(j => j.Action == "ModificationMarque"));
        }

        [Fact]
        public async Task Un_nom_de_categorie_deja_pris_est_refuse()
        {
            _context.Categories.Add(new Categorie { Id = Guid.NewGuid(), Nom = "Imprimantes" });
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ConflictException>(() => new CatalogueHandler(_context, _audit).Handle(
                new ModifierReferentielCommand(TypeReferentiel.Categorie, _categorie.Id, "imprimantes", "", true), default));
        }

        [Fact]
        public async Task Images_premiere_principale_puis_relais_a_la_suppression()
        {
            var ajouter = new AjouterImageCommandHandler(_context);
            var premiere = await ajouter.Handle(new AjouterImageCommand { ProduitId = _pc.Id, Url = Photo }, default);
            var seconde = await ajouter.Handle(new AjouterImageCommand { ProduitId = _pc.Id, Url = "https://exemple.tg/2.jpg" }, default);

            var images = await _context.ImagesProduit.AsNoTracking().ToListAsync();
            Assert.True(images.Single(i => i.Id == premiere).EstPrincipale);
            Assert.Equal(2, images.Single(i => i.Id == seconde).Ordre);

            await new CatalogueHandler(_context, _audit).Handle(new DefinirImagePrincipaleCommand(seconde), default);
            Assert.Equal(seconde, _context.ImagesProduit.Single(i => i.EstPrincipale).Id);

            await new SupprimerImageCommandHandler(_context).Handle(new SupprimerImageCommand(seconde), default);
            Assert.True(_context.ImagesProduit.Single().EstPrincipale);
        }

        [Fact]
        public void Une_image_doit_etre_une_photo_du_catalogue_ou_une_adresse_web()
        {
            var validateur = new AjouterImageCommandValidator();

            Assert.True(validateur.Validate(new AjouterImageCommand { ProduitId = _pc.Id, Url = Photo }).IsValid);
            Assert.False(validateur.Validate(new AjouterImageCommand { ProduitId = _pc.Id, Url = "/api/fichiers/paiements/0123456789abcdef0123456789abcdef.jpg" }).IsValid);
        }

        [Fact]
        public async Task Un_produit_deja_stocke_ne_se_supprime_pas()
        {
            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = "Lomé" };
            _context.Add(entrepot);
            _context.Add(new StockProduit { Id = Guid.NewGuid(), ProduitId = _pc.Id, EntrepotId = entrepot.Id });
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ConflictException>(() => new SupprimerProduitCommandHandler(_context).Handle(new SupprimerProduitCommand(_pc.Id), default));

            await new SupprimerProduitCommandHandler(_context).Handle(new SupprimerProduitCommand(_ancien.Id), default);
            Assert.False(await _context.Produits.AnyAsync(p => p.Id == _ancien.Id));
        }

        [Fact]
        public async Task Un_changement_de_prix_est_trace()
        {
            await new ModifierProduitCommandHandler(_context, _audit).Handle(new ModifierProduitCommand
            {
                Id = _pc.Id, Nom = _pc.Nom, Description = "", Prix = 280_000m, CategorieId = _categorie.Id, MarqueId = _marque.Id, Actif = true
            }, default);

            var trace = await _context.JournauxAudit.SingleAsync(j => j.Action == "ModificationProduit");
            Assert.Contains("300000", trace.AncienneValeur);
            Assert.Contains("280000", trace.NouvelleValeur);
        }

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
