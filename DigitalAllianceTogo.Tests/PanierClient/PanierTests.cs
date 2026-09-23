using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.PanierClient;
using DigitalAllianceTogo.Application.TableauDeBord.Queries;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.PanierClient
{
    /// <summary>Panier du client (§6) : demande de devis ou commande directe.</summary>
    public class PanierTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;
        private readonly Guid _pcId = Guid.NewGuid();
        private readonly Guid _sourisId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _utilisateurId = Guid.NewGuid();
        private readonly Guid _adresseId = Guid.NewGuid();

        public PanierTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _audit = new AuditService(_context, _user);

            var categorie = new Categorie { Id = Guid.NewGuid(), Nom = "Informatique" };
            var marque = new Marque { Id = Guid.NewGuid(), Nom = "HP" };
            _context.Categories.Add(categorie);
            _context.Marques.Add(marque);
            _context.Produits.AddRange(
                new Produit { Id = _pcId, Reference = "HP-250", Nom = "HP 250 G9", Prix = 300_000m, CategorieId = categorie.Id, MarqueId = marque.Id },
                new Produit { Id = _sourisId, Reference = "SOURIS", Nom = "Souris", Prix = 5_000m, CategorieId = categorie.Id, MarqueId = marque.Id });
            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = "Lomé", Adresse = "Lomé" };
            _context.Entrepots.Add(entrepot);
            _context.StocksProduit.Add(new StockProduit { Id = Guid.NewGuid(), ProduitId = _pcId, EntrepotId = entrepot.Id, QuantitePhysique = 3, QuantiteReservee = 1 });

            _context.Utilisateurs.Add(new Utilisateur { Id = _utilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _utilisateurId, Nom = "Koffi", Telephone = "+22890000000", Source = SourceClient.TikTok });
            _context.Adresses.Add(new Adresse { Id = _adresseId, ClientId = _clientId, Libelle = "Maison", Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo" });
            _context.SaveChanges();

            _user.UtilisateurId = _utilisateurId;
            _user.Roles = new[] { Roles.Client };
        }

        [Fact]
        public async Task Ajouter_modifier_retirer_avec_disponibilite()
        {
            await QuantiteAsync(_pcId, 3);
            var panier = await QuantiteAsync(_sourisId, 2);

            Assert.Equal(5, panier.NombreArticles);
            Assert.Equal(910_000m, panier.Total);
            var pc = panier.Lignes.Single(l => l.ProduitId == _pcId);
            Assert.Equal(2, pc.QuantiteDisponible); // 3 physiques − 1 réservé
            Assert.False(panier.ToutDisponible);    // 3 demandés, 2 disponibles ; souris sans stock

            panier = await QuantiteAsync(_sourisId, 0);
            Assert.Single(panier.Lignes);
        }

        [Fact]
        public async Task Un_produit_retire_de_la_vente_ne_s_ajoute_pas()
        {
            (await _context.Produits.SingleAsync(p => p.Id == _sourisId)).Actif = false;
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ConflictException>(() => QuantiteAsync(_sourisId, 1));
        }

        [Fact]
        public async Task Demande_de_devis_brouillon_pour_le_commercial_et_panier_vide()
        {
            await QuantiteAsync(_pcId, 10);

            var devisId = await Handler().Handle(new DemanderDevisDepuisPanierCommand { Commentaire = "Prix pour 10 PC ?" }, default);

            var devis = await _context.Devis.Include(d => d.Lignes).SingleAsync(d => d.Id == devisId);
            Assert.Equal(StatutDevis.Brouillon, devis.Statut);
            Assert.Equal(3_000_000m, devis.Total);
            Assert.Equal(0, devis.Remise);
            Assert.Equal("Prix pour 10 PC ?", devis.CommentaireClient);
            Assert.Equal(_utilisateurId, devis.CreeParId);
            Assert.Empty((await Handler().Handle(new GetPanierQuery(), default)).Lignes);

            // Le Commercial la voit dans ses tâches
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Commercial };
            var files = await new GetATraiterQueryHandler(_context, _user).Handle(new GetATraiterQuery(), default);
            Assert.Equal(1, files.Single(f => f.Cle == "devis-demandes-clients").Nombre);
        }

        [Fact]
        public async Task Commande_directe_au_prix_du_catalogue_actuel()
        {
            await QuantiteAsync(_pcId, 1);
            await QuantiteAsync(_sourisId, 2);
            // Le prix change après l'ajout au panier : c'est le prix du jour qui compte
            (await _context.Produits.SingleAsync(p => p.Id == _pcId)).Prix = 280_000m;
            await _context.SaveChangesAsync();

            var commandeId = await Handler().Handle(new CommanderPanierCommand { AdresseLivraisonId = _adresseId }, default);

            var commande = await _context.Commandes.Include(c => c.Versions).ThenInclude(v => v.Lignes).Include(c => c.AdresseLivraison)
                .SingleAsync(c => c.Id == commandeId);
            Assert.Equal(StatutCommande.CommandeCreee, commande.Statut);
            Assert.Null(commande.DevisOrigineId);
            Assert.Equal(290_000m, commande.Versions.Single().Total);
            Assert.Equal("+22890000000", commande.AdresseLivraison.TelephoneContact);
            Assert.Empty((await Handler().Handle(new GetPanierQuery(), default)).Lignes);
        }

        [Fact]
        public async Task Garde_fous_panier_vide_adresse_d_un_autre_et_compte_sans_fiche_client()
        {
            await Assert.ThrowsAsync<ConflictException>(() => Handler().Handle(new CommanderPanierCommand { AdresseLivraisonId = _adresseId }, default));

            await QuantiteAsync(_pcId, 1);
            await Assert.ThrowsAsync<NotFoundException>(() => Handler().Handle(new CommanderPanierCommand { AdresseLivraisonId = Guid.NewGuid() }, default));

            _user.UtilisateurId = Guid.NewGuid(); // compte sans fiche client
            await QuantiteAsync(_pcId, 1);
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => Handler().Handle(new DemanderDevisDepuisPanierCommand(), default));
        }

        private PanierHandler Handler() => new(_context, _user, _audit);

        private Task<PanierDto> QuantiteAsync(Guid produitId, int quantite) =>
            Handler().Handle(new DefinirQuantitePanierCommand(produitId, quantite), default);

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
