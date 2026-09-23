using DigitalAllianceTogo.Application.Commandes.Commands.Preparation;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Livraisons.Commands.ConfirmerLivraison;
using DigitalAllianceTogo.Application.Livraisons.Commands.PlanifierLivraison;
using DigitalAllianceTogo.Application.Livraisons.Commands.RemettreAuLivreur;
using DigitalAllianceTogo.Application.Livraisons.Commands.SignalerEchecLivraison;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Tests.Livraisons
{
    /// <summary>
    /// Préparation → livraison (cahier des charges §12) et ses effets sur le stock :
    /// la sortie a lieu à la remise au livreur, une seule fois.
    /// </summary>
    public class LivraisonWorkflowTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;

        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _stockId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _livreurId = Guid.NewGuid();
        private readonly Guid _autreLivreurId = Guid.NewGuid();
        private readonly Guid _gestionnaireId = Guid.NewGuid();
        private readonly Guid _commandeId;

        public LivraisonWorkflowTests()
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

            var roleLivreur = new Role { Id = Guid.NewGuid(), Nom = Roles.Livreur };
            _context.Roles.Add(roleLivreur);
            foreach (var id in new[] { _livreurId, _autreLivreurId })
            {
                _context.Utilisateurs.Add(new Utilisateur { Id = id, Nom = "Livreur", Prenom = "Kossi", Email = $"{id}@test.tg" });
                _context.UtilisateurRoles.Add(new UtilisateurRole { Id = Guid.NewGuid(), UtilisateurId = id, RoleId = roleLivreur.Id });
            }
            _context.Utilisateurs.Add(new Utilisateur { Id = _gestionnaireId, Nom = "Stock", Prenom = "Gestion", Email = "stock@test.tg" });

            var utilisateurClientId = Guid.NewGuid();
            _context.Utilisateurs.Add(new Utilisateur { Id = utilisateurClientId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = utilisateurClientId, Nom = "Koffi", Prenom = "Ama", Telephone = "+22890000000" });

            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = "Lomé", Adresse = "Lomé" };
            _context.Entrepots.Add(entrepot);
            // 5 PC en stock, dont 2 déjà réservés pour la commande (paiement confirmé)
            _context.StocksProduit.Add(new StockProduit { Id = _stockId, ProduitId = _produitId, EntrepotId = entrepot.Id, QuantitePhysique = 5, QuantiteReservee = 2 });

            var commande = new Commande
            {
                Id = Guid.NewGuid(),
                Reference = "CMD-TEST",
                Statut = StatutCommande.StockReserve,
                ClientId = _clientId,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+22890000000" }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = 600_000m, Total = 600_000m };
            version.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _produitId, Quantite = 2, PrixUnitaire = 300_000m, Total = 600_000m });
            commande.Versions.Add(version);
            _context.Commandes.Add(commande);
            _context.MouvementsStock.Add(new MouvementStock
            {
                Id = Guid.NewGuid(), Type = TypeMouvementStock.Reservation, Quantite = 2, Reference = commande.Reference,
                CommandeId = commande.Id, StockProduitId = _stockId
            });
            _context.SaveChanges();
            _commandeId = commande.Id;
        }

        // ---------- Parcours nominal ----------

        [Fact]
        public async Task Parcours_complet_sortie_a_la_remise_puis_livraison()
        {
            var livraisonId = await PreparerEtPlanifierAsync();

            // Planifiée : le stock n'a pas bougé
            await AssertStockAsync(physique: 5, reserve: 2, transit: 0);

            await RemettreAsync(livraisonId);

            await AssertStockAsync(physique: 3, reserve: 0, transit: 2);
            Assert.Equal(StatutCommande.EnTransit, (await CommandeAsync()).Statut);
            var livraison = await LivraisonAsync(livraisonId);
            Assert.Equal(StatutLivraison.EnTransit, livraison.Statut);
            Assert.NotNull(livraison.DatePriseEnCharge);
            Assert.Equal(2, await _context.MouvementsStock.Where(m => m.Type == TypeMouvementStock.Sortie && m.CommandeId == _commandeId).SumAsync(m => m.Quantite));

            ConnecterLivreur(_livreurId);
            await new ConfirmerLivraisonCommandHandler(_context, _user, _audit).Handle(new ConfirmerLivraisonCommand
            {
                Id = livraisonId, PhotoUrl = "https://photos.tg/colis.jpg", Latitude = 6.1375m, Longitude = 1.2123m
            }, default);

            await AssertStockAsync(physique: 3, reserve: 0, transit: 0);
            Assert.Equal(StatutCommande.Livree, (await CommandeAsync()).Statut);
            livraison = await _context.Livraisons.Include(l => l.Preuve).SingleAsync(l => l.Id == livraisonId);
            Assert.Equal(StatutLivraison.Livree, livraison.Statut);
            Assert.NotNull(livraison.DateLivraison);
            Assert.Equal("https://photos.tg/colis.jpg", livraison.Preuve!.PhotoUrl);
        }

        [Fact]
        public async Task La_sortie_de_stock_n_a_lieu_qu_une_fois()
        {
            var livraisonId = await PreparerEtPlanifierAsync();
            await RemettreAsync(livraisonId);

            await Assert.ThrowsAsync<ConflictException>(() => RemettreAsync(livraisonId));
            await AssertStockAsync(physique: 3, reserve: 0, transit: 2);
        }

        [Fact]
        public async Task Livraison_avec_reserve_livre_la_commande_sans_ouvrir_de_SAV()
        {
            var livraisonId = await PreparerEtPlanifierAsync();
            await RemettreAsync(livraisonId);

            ConnecterLivreur(_livreurId);
            await new ConfirmerLivraisonCommandHandler(_context, _user, _audit).Handle(new ConfirmerLivraisonCommand
            {
                Id = livraisonId, SignatureUrl = "https://photos.tg/signature.png", Reserve = "Carton enfoncé"
            }, default);

            var livraison = await LivraisonAsync(livraisonId);
            Assert.Equal(StatutLivraison.LivreeAvecReserve, livraison.Statut);
            Assert.Equal("Carton enfoncé", livraison.Reserve);
            Assert.Equal(StatutCommande.Livree, (await CommandeAsync()).Statut);
            Assert.False(await _context.TicketsSAV.AnyAsync());
        }

        [Fact]
        public void Une_preuve_photo_ou_signature_est_obligatoire()
        {
            var resultat = new ConfirmerLivraisonCommandValidator().Validate(new ConfirmerLivraisonCommand { Id = Guid.NewGuid() });

            Assert.False(resultat.IsValid);
        }

        // ---------- Échecs ----------

        [Fact]
        public async Task Client_absent_retour_au_depot_reservation_maintenue_puis_relivraison()
        {
            var livraisonId = await PreparerEtPlanifierAsync();
            await RemettreAsync(livraisonId);

            await EchouerAsync(livraisonId, refus: false);

            await AssertStockAsync(physique: 5, reserve: 2, transit: 0);
            Assert.Equal(StatutCommande.PretePourLivraison, (await CommandeAsync()).Statut);
            Assert.Equal(StatutLivraison.AReprogrammer, (await LivraisonAsync(livraisonId)).Statut);

            // Nouvelle tentative : Relivraison, et une nouvelle sortie de la même quantité
            var relivraisonId = await PlanifierAsync(_livreurId);
            Assert.Equal(TypeLivraison.Relivraison, (await LivraisonAsync(relivraisonId)).Type);
            await RemettreAsync(relivraisonId);
            await AssertStockAsync(physique: 3, reserve: 0, transit: 2);
        }

        [Fact]
        public async Task Refus_client_le_colis_reste_en_transit_jusqu_au_controle_du_stock()
        {
            var livraisonId = await PreparerEtPlanifierAsync();
            await RemettreAsync(livraisonId);

            await EchouerAsync(livraisonId, refus: true);

            // §16 : rien ne redevient vendable avant la réception et le contrôle par le Gestionnaire de stock
            await AssertStockAsync(physique: 3, reserve: 0, transit: 2);
            Assert.Equal(StatutCommande.LivraisonEchoueeRefusClient, (await CommandeAsync()).Statut);
            var livraison = await LivraisonAsync(livraisonId);
            Assert.Equal(StatutLivraison.Echouee, livraison.Statut);
            Assert.Equal("Motif de test", livraison.MotifEchec);
        }

        // ---------- Garde-fous ----------

        [Fact]
        public async Task Impossible_de_planifier_une_commande_non_preparee()
        {
            await Assert.ThrowsAsync<ConflictException>(() => PlanifierAsync(_livreurId));
        }

        [Fact]
        public async Task Seul_un_livreur_actif_peut_etre_assigne()
        {
            await PreparerAsync();

            await Assert.ThrowsAsync<ConflictException>(() => PlanifierAsync(_gestionnaireId));
        }

        [Fact]
        public async Task Une_seule_livraison_en_cours_par_commande()
        {
            await PreparerEtPlanifierAsync();

            await Assert.ThrowsAsync<ConflictException>(() => PlanifierAsync(_autreLivreurId));
        }

        [Fact]
        public async Task Un_livreur_n_agit_que_sur_ses_livraisons()
        {
            var livraisonId = await PreparerEtPlanifierAsync();
            await RemettreAsync(livraisonId);

            ConnecterLivreur(_autreLivreurId);
            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new ConfirmerLivraisonCommandHandler(_context, _user, _audit).Handle(new ConfirmerLivraisonCommand
                {
                    Id = livraisonId, PhotoUrl = "https://photos.tg/x.jpg"
                }, default));
        }

        [Fact]
        public async Task Liste_des_livreurs_actifs_avec_leur_charge()
        {
            await PreparerEtPlanifierAsync();
            var inactif = await _context.Utilisateurs.SingleAsync(u => u.Id == _autreLivreurId);
            inactif.Actif = false;
            await _context.SaveChangesAsync();

            var livreurs = await new DigitalAllianceTogo.Application.Livraisons.Queries.GetLivreursQueryHandler(_context)
                .Handle(new DigitalAllianceTogo.Application.Livraisons.Queries.GetLivreursQuery(), default);

            var livreur = Assert.Single(livreurs); // ni le gestionnaire, ni le livreur désactivé
            Assert.Equal(_livreurId, livreur.Id);
            Assert.Equal(1, livreur.LivraisonsEnCours);
        }

        // ---------- Utilitaires ----------

        private async Task PreparerAsync()
        {
            ConnecterGestionnaire();
            var handler = new PreparationCommandsHandler(_context, _audit);
            await handler.Handle(new DemarrerPreparationCommand(_commandeId), default);
            await handler.Handle(new TerminerPreparationCommand(_commandeId), default);
        }

        private async Task<Guid> PreparerEtPlanifierAsync()
        {
            await PreparerAsync();
            return await PlanifierAsync(_livreurId);
        }

        private Task<Guid> PlanifierAsync(Guid livreurId)
        {
            ConnecterGestionnaire();
            return new PlanifierLivraisonCommandHandler(_context, _audit).Handle(new PlanifierLivraisonCommand
            {
                CommandeId = _commandeId, LivreurId = livreurId, DatePlanifiee = DateTime.UtcNow.AddDays(1)
            }, default);
        }

        private Task RemettreAsync(Guid livraisonId)
        {
            ConnecterGestionnaire();
            return new RemettreAuLivreurCommandHandler(_context, _user, _audit).Handle(new RemettreAuLivreurCommand(livraisonId), default);
        }

        private Task EchouerAsync(Guid livraisonId, bool refus)
        {
            ConnecterLivreur(_livreurId);
            return new SignalerEchecLivraisonCommandHandler(_context, _user, _audit).Handle(new SignalerEchecLivraisonCommand
            {
                Id = livraisonId, Motif = "Motif de test", RefusClient = refus
            }, default);
        }

        private async Task AssertStockAsync(int physique, int reserve, int transit)
        {
            var stock = await _context.StocksProduit.SingleAsync(s => s.Id == _stockId);
            Assert.Equal(physique, stock.QuantitePhysique);
            Assert.Equal(reserve, stock.QuantiteReservee);
            Assert.Equal(transit, stock.QuantiteEnTransit);
        }

        private Task<Commande> CommandeAsync() => _context.Commandes.SingleAsync(c => c.Id == _commandeId);

        private Task<LivraisonEntity> LivraisonAsync(Guid id) => _context.Livraisons.SingleAsync(l => l.Id == id);

        private void ConnecterGestionnaire()
        {
            _user.UtilisateurId = _gestionnaireId;
            _user.Roles = new[] { Roles.GestionnaireStock };
        }

        private void ConnecterLivreur(Guid livreurId)
        {
            _user.UtilisateurId = livreurId;
            _user.Roles = new[] { Roles.Livreur };
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
