using DigitalAllianceTogo.Application.Commandes.Commands.CloturerCommande;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Finance.Commands;
using DigitalAllianceTogo.Application.Livraisons.Commands.ConfirmerLivraison;
using DigitalAllianceTogo.Application.Livraisons.Commands.RemettreAuLivreur;
using DigitalAllianceTogo.Application.Sav.Commands.CreerTicketSav;
using DigitalAllianceTogo.Application.Sav.Commands.DeciderSav;
using DigitalAllianceTogo.Application.Sav.Commands.Logistique;
using DigitalAllianceTogo.Application.Sav.Commands.Technique;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Sav
{
    /// <summary>
    /// SAV (cahier des charges §24-27) : réparation, décision commerciale, remplacement
    /// avec livraison dédiée, remboursement / avoir. La commande n'est jamais rouverte.
    /// </summary>
    public class SavWorkflowTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;

        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _entrepotId = Guid.NewGuid();
        private readonly Guid _stockId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _clientUtilisateurId = Guid.NewGuid();
        private readonly Guid _technicienId = Guid.NewGuid();
        private readonly Guid _livreurId = Guid.NewGuid();
        private readonly Guid _commandeId = Guid.NewGuid();
        private readonly Guid _ligneId = Guid.NewGuid();

        public SavWorkflowTests()
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
            _context.Utilisateurs.Add(new Utilisateur { Id = _livreurId, Nom = "Livreur", Prenom = "Kossi", Email = "livreur@test.tg" });
            _context.UtilisateurRoles.Add(new UtilisateurRole { Id = Guid.NewGuid(), UtilisateurId = _livreurId, RoleId = roleLivreur.Id });
            _context.Utilisateurs.Add(new Utilisateur { Id = _technicienId, Nom = "Tech", Prenom = "Yao", Email = "tech@test.tg" });
            _context.Utilisateurs.Add(new Utilisateur { Id = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Telephone = "+22890000000" });

            _context.Entrepots.Add(new Entrepot { Id = _entrepotId, Nom = "Lomé", Adresse = "Lomé" });
            _context.StocksProduit.Add(new StockProduit { Id = _stockId, ProduitId = _produitId, EntrepotId = _entrepotId, QuantitePhysique = 3 });

            // Commande livrée : 2 PC à 300 000, remise globale de 60 000 (payé 540 000 => 270 000 par PC)
            var commande = new Commande
            {
                Id = _commandeId, Reference = "CMD-SAV", Statut = StatutCommande.Livree, ClientId = _clientId,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+22890000000" }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = 600_000m, Remise = 60_000m, Total = 540_000m };
            version.Lignes.Add(new LigneCommande { Id = _ligneId, ProduitId = _produitId, Quantite = 2, PrixUnitaire = 300_000m, Total = 600_000m });
            commande.Versions.Add(version);
            _context.Commandes.Add(commande);
            _context.SaveChanges();
        }

        // ---------- Ouverture ----------

        [Fact]
        public async Task Le_client_ouvre_un_ticket_sans_rouvrir_la_commande()
        {
            var ticketId = await OuvrirAsync(quantite: 1, parLeClient: true);

            var ticket = await _context.TicketsSAV.SingleAsync(t => t.Id == ticketId);
            Assert.Equal(StatutSav.Ouvert, ticket.Statut);
            Assert.Equal(_clientId, ticket.ClientId);
            Assert.Equal(StatutCommande.Livree, (await CommandeAsync()).Statut);
        }

        [Fact]
        public async Task Pas_de_SAV_avant_livraison()
        {
            (await CommandeAsync()).Statut = StatutCommande.EnTransit;
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ConflictException>(() => OuvrirAsync(quantite: 1));
        }

        [Fact]
        public async Task Pas_plus_d_unites_en_SAV_que_commandees()
        {
            await OuvrirAsync(quantite: 2);

            await Assert.ThrowsAsync<ConflictException>(() => OuvrirAsync(quantite: 1));
        }

        [Fact]
        public async Task Un_client_n_ouvre_pas_de_ticket_sur_la_commande_d_un_autre()
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Client };

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new CreerTicketSavCommandHandler(_context, _user, _audit).Handle(new CreerTicketSavCommand { LigneCommandeId = _ligneId, Motif = "x" }, default));
        }

        // ---------- Réparation (§26) ----------

        [Fact]
        public async Task Produit_reparable_repare_et_teste_cloture_sans_admin()
        {
            var ticketId = await OuvrirAsync(quantite: 1);

            await DiagnostiquerAsync(ticketId, reparable: true);
            Assert.Equal(StatutSav.EnReparation, (await TicketAsync(ticketId)).Statut);

            ConnecterTechnicien();
            await new TechniqueCommandsHandler(_context, _user, _audit).Handle(new TerminerReparationCommand
            {
                Id = ticketId, Description = "Remplacement de la nappe écran", Resultat = "Affichage OK", TestReussi = true
            }, default);

            var ticket = await _context.TicketsSAV.Include(t => t.Diagnostics).Include(t => t.Interventions).SingleAsync(t => t.Id == ticketId);
            Assert.Equal(StatutSav.Cloture, ticket.Statut);
            Assert.Equal("Réparé et testé", ticket.Resolution);
            Assert.Equal(_technicienId, ticket.TechnicienId);
            Assert.Single(ticket.Diagnostics);
            Assert.Single(ticket.Interventions);
            Assert.Null(ticket.Decision);
        }

        [Fact]
        public void Un_produit_irreparable_exige_une_recommandation()
        {
            var resultat = new DiagnostiquerTicketCommandValidator().Validate(new DiagnostiquerTicketCommand
            {
                Id = Guid.NewGuid(), Conclusion = "Carte mère grillée", Reparable = false
            });

            Assert.False(resultat.IsValid);
        }

        // ---------- Remboursement / avoir ----------

        [Fact]
        public async Task Remboursement_SAV_au_prix_paye_puis_cloture_du_ticket()
        {
            var ticketId = await IrreparableAsync();

            var result = await DeciderAsync(ticketId, DecisionSav.Remboursement);

            Assert.Equal(270_000m, result.MontantARegulariser); // remise globale répartie au prorata
            Assert.Equal(StatutSav.EnAttenteRegulationFinanciere, (await TicketAsync(ticketId)).Statut);
            var remboursement = await _context.Remboursements.SingleAsync();
            Assert.Equal(ticketId, remboursement.TicketSAVId);
            Assert.Equal(_commandeId, remboursement.CommandeId);

            // Le SAV ne bloque pas la clôture de la commande, qui ne bouge pas
            ConnecterAdmin();
            await new CloturerCommandeCommandHandler(_context, _user, _audit).Handle(new CloturerCommandeCommand { Id = _commandeId }, default);
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync()).Statut);

            var finance = new RemboursementCommandsHandler(_context, _audit);
            await finance.Handle(new ValiderRemboursementCommand(remboursement.Id), default);
            await finance.Handle(new ExecuterRemboursementCommand { Id = remboursement.Id, Reussi = true, ReferenceTransaction = "TX-SAV-1" }, default);

            var ticket = await TicketAsync(ticketId);
            Assert.Equal(StatutSav.Cloture, ticket.Statut);
            Assert.Equal("Remboursé", ticket.Resolution);
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync()).Statut);
        }

        // ---------- Remplacement (§27) ----------

        [Fact]
        public async Task Remplacement_complet_avec_livraison_dediee_et_controle_de_l_ancien_produit()
        {
            var ticketId = await IrreparableAsync();

            var decision = await DeciderAsync(ticketId, DecisionSav.Remplacement);
            Assert.True(decision.StockReserve);
            await AssertStockAsync(physique: 3, reserve: 1, transit: 0, defectueux: 0);

            ConnecterGestionnaire();
            var livraisonId = await new LogistiqueSavCommandsHandler(_context, _user, _audit).Handle(new PlanifierLivraisonSavCommand
            {
                TicketId = ticketId, LivreurId = _livreurId, DatePlanifiee = DateTime.UtcNow.AddDays(1)
            }, default);
            var livraison = await _context.Livraisons.SingleAsync(l => l.Id == livraisonId);
            Assert.Equal(TypeLivraison.RemplacementSav, livraison.Type);
            Assert.Equal(ticketId, livraison.TicketSAVId);

            await new RemettreAuLivreurCommandHandler(_context, _user, _audit).Handle(new RemettreAuLivreurCommand(livraisonId), default);
            await AssertStockAsync(physique: 2, reserve: 0, transit: 1, defectueux: 0);
            Assert.Equal(StatutCommande.Livree, (await CommandeAsync()).Statut); // commande jamais rouverte

            _user.UtilisateurId = _livreurId;
            _user.Roles = new[] { Roles.Livreur };
            await new ConfirmerLivraisonCommandHandler(_context, _user, _audit).Handle(new ConfirmerLivraisonCommand
            {
                Id = livraisonId, SignatureUrl = "https://photos.tg/signature.png"
            }, default);

            var ticket = await TicketAsync(ticketId);
            Assert.Equal(StatutSav.Cloture, ticket.Statut);
            Assert.Equal("Produit remplacé", ticket.Resolution);
            await AssertStockAsync(physique: 2, reserve: 0, transit: 0, defectueux: 0);
            Assert.Equal(StatutCommande.Livree, (await CommandeAsync()).Statut);

            // L'ancien produit revient : le stock le classe défectueux
            ConnecterGestionnaire();
            var logistique = new LogistiqueSavCommandsHandler(_context, _user, _audit);
            await logistique.Handle(new ReceptionnerAncienProduitCommand { TicketId = ticketId, EntrepotId = _entrepotId, Defectueux = true }, default);
            await AssertStockAsync(physique: 2, reserve: 0, transit: 0, defectueux: 1);
            await Assert.ThrowsAsync<ConflictException>(() =>
                logistique.Handle(new ReceptionnerAncienProduitCommand { TicketId = ticketId, EntrepotId = _entrepotId }, default));
        }

        [Fact]
        public async Task Remplacement_en_rupture_puis_changement_de_decision_libere_le_stock()
        {
            var ticketId = await IrreparableAsync();
            (await _context.StocksProduit.SingleAsync(s => s.Id == _stockId)).QuantitePhysique = 0;
            await _context.SaveChangesAsync();

            var decision = await DeciderAsync(ticketId, DecisionSav.Remplacement);
            Assert.False(decision.StockReserve);

            ConnecterGestionnaire();
            await Assert.ThrowsAsync<ConflictException>(() =>
                new LogistiqueSavCommandsHandler(_context, _user, _audit).Handle(new PlanifierLivraisonSavCommand
                {
                    TicketId = ticketId, LivreurId = _livreurId, DatePlanifiee = DateTime.UtcNow.AddDays(1)
                }, default));

            // Stock reçu entre-temps et réservé, puis le client préfère un avoir
            (await _context.StocksProduit.SingleAsync(s => s.Id == _stockId)).QuantitePhysique = 3;
            await _context.SaveChangesAsync();
            await Assert.ThrowsAsync<ConflictException>(() => DeciderAsync(ticketId, DecisionSav.Remplacement)); // déjà décidé
            ConnecterGestionnaire();
            var livraisonId = await new LogistiqueSavCommandsHandler(_context, _user, _audit).Handle(new PlanifierLivraisonSavCommand
            {
                TicketId = ticketId, LivreurId = _livreurId, DatePlanifiee = DateTime.UtcNow.AddDays(1)
            }, default);
            await AssertStockAsync(physique: 3, reserve: 1, transit: 0, defectueux: 0);

            // Livraison planifiée : on ne peut plus changer d'avis
            await Assert.ThrowsAsync<ConflictException>(() => DeciderAsync(ticketId, DecisionSav.Avoir));

            // Livraison annulée (échec avant remise simulé) : changement possible, réservation libérée
            (await _context.Livraisons.SingleAsync(l => l.Id == livraisonId)).Statut = StatutLivraison.Echouee;
            await _context.SaveChangesAsync();
            var avoir = await DeciderAsync(ticketId, DecisionSav.Avoir);

            Assert.Equal(StatutSav.EnAttenteRegulationFinanciere.ToString(), avoir.Statut);
            await AssertStockAsync(physique: 3, reserve: 0, transit: 0, defectueux: 0);
        }

        // ---------- Utilitaires ----------

        private Task<Guid> OuvrirAsync(int quantite, bool parLeClient = false)
        {
            if (parLeClient)
            {
                _user.UtilisateurId = _clientUtilisateurId;
                _user.Roles = new[] { Roles.Client };
            }
            else
            {
                _user.UtilisateurId = Guid.NewGuid();
                _user.Roles = new[] { Roles.Commercial };
            }
            return new CreerTicketSavCommandHandler(_context, _user, _audit).Handle(new CreerTicketSavCommand
            {
                LigneCommandeId = _ligneId, Quantite = quantite, Motif = "L'écran ne s'allume plus"
            }, default);
        }

        private Task DiagnostiquerAsync(Guid ticketId, bool reparable)
        {
            ConnecterTechnicien();
            return new TechniqueCommandsHandler(_context, _user, _audit).Handle(new DiagnostiquerTicketCommand
            {
                Id = ticketId,
                Conclusion = reparable ? "Nappe écran défectueuse" : "Carte mère grillée",
                Reparable = reparable,
                Recommandation = reparable ? null : "Remplacement"
            }, default);
        }

        private async Task<Guid> IrreparableAsync()
        {
            var ticketId = await OuvrirAsync(quantite: 1);
            await DiagnostiquerAsync(ticketId, reparable: false);
            Assert.Equal(StatutSav.DecisionCommerciale, (await TicketAsync(ticketId)).Statut);
            return ticketId;
        }

        private Task<DeciderSavResult> DeciderAsync(Guid ticketId, DecisionSav decision)
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Commercial };
            return new DeciderSavCommandHandler(_context, _user, _audit).Handle(new DeciderSavCommand { Id = ticketId, Decision = decision }, default);
        }

        private async Task AssertStockAsync(int physique, int reserve, int transit, int defectueux)
        {
            var stock = await _context.StocksProduit.SingleAsync(s => s.Id == _stockId);
            Assert.Equal(physique, stock.QuantitePhysique);
            Assert.Equal(reserve, stock.QuantiteReservee);
            Assert.Equal(transit, stock.QuantiteEnTransit);
            Assert.Equal(defectueux, stock.QuantiteDefectueuse);
        }

        private Task<Commande> CommandeAsync() => _context.Commandes.SingleAsync(c => c.Id == _commandeId);

        private Task<Domain.Models.SAV.TicketSAV> TicketAsync(Guid id) => _context.TicketsSAV.SingleAsync(t => t.Id == id);

        private void ConnecterTechnicien()
        {
            _user.UtilisateurId = _technicienId;
            _user.Roles = new[] { Roles.Technicien };
        }

        private void ConnecterGestionnaire()
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.GestionnaireStock };
        }

        private void ConnecterAdmin()
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Admin };
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
