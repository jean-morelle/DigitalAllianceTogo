using DigitalAllianceTogo.Application.Commandes.Commands.AnnulerCommande;
using DigitalAllianceTogo.Application.Commandes.Commands.CloturerCommande;
using DigitalAllianceTogo.Application.Commandes.Commands.Preparation;
using DigitalAllianceTogo.Application.Commandes.Commands.ReceptionnerRetour;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Finance.Commands;
using DigitalAllianceTogo.Application.Livraisons.Commands.PlanifierLivraison;
using DigitalAllianceTogo.Application.Livraisons.Commands.RemettreAuLivreur;
using DigitalAllianceTogo.Application.Livraisons.Commands.SignalerEchecLivraison;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Finance;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Finance
{
    /// <summary>
    /// Annulation (§18), refus de livraison (§16), remboursement / avoir (§22-23) et clôture (§17).
    /// </summary>
    public class RegularisationTests
    {
        private const decimal Montant = 600_000m;

        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;

        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _stockId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _clientUtilisateurId = Guid.NewGuid();
        private readonly Guid _livreurId = Guid.NewGuid();
        private readonly Guid _adminId = Guid.NewGuid();

        public RegularisationTests()
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
            _context.Utilisateurs.Add(new Utilisateur { Id = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Telephone = "+22890000000" });

            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = "Lomé", Adresse = "Lomé" };
            _context.Entrepots.Add(entrepot);
            _context.StocksProduit.Add(new StockProduit { Id = _stockId, ProduitId = _produitId, EntrepotId = entrepot.Id, QuantitePhysique = 5 });
            _context.SaveChanges();
        }

        // ---------- Annulation selon le moment (§18) ----------

        [Fact]
        public async Task Annulation_avant_paiement_cloture_directement()
        {
            var commandeId = CreerCommande(StatutCommande.CommandeCreee, payee: false);

            var result = await AnnulerAsync(commandeId, estClient: true);

            Assert.Equal(StatutCommande.Cloturee.ToString(), result.Statut);
            Assert.Equal(0, result.MontantARegulariser);
            Assert.False(await _context.Remboursements.AnyAsync());
        }

        [Fact]
        public async Task Pas_d_annulation_pendant_la_verification_du_paiement()
        {
            var commandeId = CreerCommande(StatutCommande.PaiementEnAttente, payee: false);

            await Assert.ThrowsAsync<ConflictException>(() => AnnulerAsync(commandeId, estClient: true));
        }

        [Fact]
        public async Task Un_client_n_annule_pas_la_commande_d_un_autre()
        {
            var commandeId = CreerCommande(StatutCommande.CommandeCreee, payee: false);
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Client };

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new AnnulerCommandeCommandHandler(_context, _user, _audit).Handle(new AnnulerCommandeCommand { Id = commandeId, Motif = "x" }, default));
        }

        [Fact]
        public async Task Annulation_apres_paiement_libere_le_stock_et_demande_un_remboursement()
        {
            var commandeId = CreerCommande(StatutCommande.StockReserve);

            var result = await AnnulerAsync(commandeId, estClient: true);

            Assert.Equal(StatutCommande.EnAttenteRegulationFinanciere.ToString(), result.Statut);
            Assert.Equal(Montant, result.MontantARegulariser);
            await AssertStockAsync(physique: 5, reserve: 0, transit: 0, defectueux: 0);
            var remboursement = await _context.Remboursements.SingleAsync();
            Assert.Equal(StatutRemboursement.EnAttente, remboursement.Statut);
            Assert.Equal(Montant, remboursement.Montant);
            Assert.StartsWith("Annulation", remboursement.Motif);
        }

        [Fact]
        public async Task Annulation_refusee_en_transit_et_apres_livraison()
        {
            await Assert.ThrowsAsync<ConflictException>(() => AnnulerAsync(CreerCommande(StatutCommande.EnTransit), estClient: true));
            await Assert.ThrowsAsync<ConflictException>(() => AnnulerAsync(CreerCommande(StatutCommande.Livree), estClient: true));
        }

        [Fact]
        public async Task Annulation_pendant_la_preparation_attend_le_controle_du_stock()
        {
            var commandeId = CreerCommande(StatutCommande.StockReserve);
            await PreparerAsync(commandeId);
            var livraisonId = await PlanifierAsync(commandeId);

            var result = await AnnulerAsync(commandeId, estClient: false);

            Assert.Equal(StatutCommande.AnnulationEnCours.ToString(), result.Statut);
            Assert.Equal(StatutLivraison.Echouee, (await _context.Livraisons.SingleAsync(l => l.Id == livraisonId)).Statut);
            await AssertStockAsync(physique: 5, reserve: 2, transit: 0, defectueux: 0); // toujours réservé tant que non contrôlé

            // Contrôle : 1 abîmé, 1 intact
            await ReceptionnerAsync(commandeId, defectueux: 1);

            await AssertStockAsync(physique: 4, reserve: 0, transit: 0, defectueux: 1);
            Assert.Equal(StatutCommande.EnAttenteRegulationFinanciere, (await CommandeAsync(commandeId)).Statut);
        }

        // ---------- Refus de livraison (§16) ----------

        [Fact]
        public async Task Refus_de_livraison_retour_controle_puis_remboursement()
        {
            var commandeId = CreerCommande(StatutCommande.StockReserve);
            await PreparerAsync(commandeId);
            var livraisonId = await PlanifierAsync(commandeId);
            await RemettreAsync(livraisonId);

            _user.UtilisateurId = _livreurId;
            _user.Roles = new[] { Roles.Livreur };
            await new SignalerEchecLivraisonCommandHandler(_context, _user, _audit).Handle(new SignalerEchecLivraisonCommand
            {
                Id = livraisonId, Motif = "Le client ne veut plus le produit", RefusClient = true
            }, default);

            // Demande de remboursement créée dès le refus ; colis encore en transit
            Assert.Equal(StatutRemboursement.EnAttente, (await _context.Remboursements.SingleAsync()).Statut);
            await AssertStockAsync(physique: 3, reserve: 0, transit: 2, defectueux: 0);

            await ReceptionnerAsync(commandeId, defectueux: 1);

            await AssertStockAsync(physique: 4, reserve: 0, transit: 0, defectueux: 1);
            Assert.Equal(StatutLivraison.Retournee, (await _context.Livraisons.SingleAsync(l => l.Id == livraisonId)).Statut);
            Assert.Equal(StatutCommande.EnAttenteRegulationFinanciere, (await CommandeAsync(commandeId)).Statut);
        }

        [Fact]
        public async Task On_ne_declare_pas_plus_de_defectueux_que_de_produits_retournes()
        {
            var commandeId = CreerCommande(StatutCommande.StockReserve);
            await PreparerAsync(commandeId);
            await AnnulerAsync(commandeId, estClient: false);

            await Assert.ThrowsAsync<ConflictException>(() => ReceptionnerAsync(commandeId, defectueux: 3));
        }

        // ---------- Remboursement (§22) ----------

        [Fact]
        public async Task Remboursement_echoue_bloque_la_cloture_puis_execution_cloture()
        {
            var commandeId = CreerCommande(StatutCommande.StockReserve);
            await AnnulerAsync(commandeId, estClient: true);
            var remboursementId = (await _context.Remboursements.SingleAsync()).Id;
            var handler = new RemboursementCommandsHandler(_context, _audit);
            ConnecterAdmin();

            // Pas d'exécution sans validation de l'Administrateur
            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ExecuterRemboursementCommand { Id = remboursementId, Reussi = true, ReferenceTransaction = "TX" }, default));

            await handler.Handle(new ValiderRemboursementCommand(remboursementId), default);
            await handler.Handle(new ExecuterRemboursementCommand { Id = remboursementId, Reussi = false, MotifEchec = "Numéro Flooz invalide" }, default);

            Assert.Equal(StatutRemboursement.Echoue, (await _context.Remboursements.SingleAsync()).Statut);
            Assert.Equal(StatutCommande.EnAttenteRegulationFinanciere, (await CommandeAsync(commandeId)).Statut);
            await Assert.ThrowsAsync<ConflictException>(() => CloturerAsync(commandeId, Roles.Admin, "Forcer"));

            // Nouvelle tentative réussie : la commande se clôture d'elle-même
            await handler.Handle(new ExecuterRemboursementCommand { Id = remboursementId, Reussi = true, ReferenceTransaction = "FLOOZ-TX-42" }, default);

            var remboursement = await _context.Remboursements.SingleAsync();
            Assert.Equal(StatutRemboursement.Execute, remboursement.Statut);
            Assert.Equal("FLOOZ-TX-42", remboursement.ReferenceTransaction);
            Assert.NotNull(remboursement.DateExecution);
            Assert.Null(remboursement.MotifEchec);
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync(commandeId)).Statut);
        }

        // ---------- Avoir (§23) ----------

        [Fact]
        public async Task Avoir_valide_devient_disponible_et_cloture_la_commande()
        {
            var commandeId = CreerCommande(StatutCommande.PaiementConfirme);
            await AnnulerAsync(commandeId, estClient: true, ModeRegularisation.Avoir);
            var avoir = await _context.Avoirs.SingleAsync();
            Assert.Equal(StatutAvoir.EnAttente, avoir.Statut);
            Assert.False(await _context.Remboursements.AnyAsync());

            ConnecterAdmin();
            await new AvoirCommandsHandler(_context, _audit).Handle(new ValiderAvoirCommand(avoir.Id), default);

            Assert.Equal(StatutAvoir.Disponible, (await _context.Avoirs.SingleAsync()).Statut);
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync(commandeId)).Statut);
        }

        // ---------- Clôture (§17) ----------

        [Fact]
        public async Task Commande_livree_cloturee_par_le_commercial()
        {
            var commandeId = CreerCommande(StatutCommande.Livree);

            await CloturerAsync(commandeId, Roles.Commercial);

            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync(commandeId)).Statut);
        }

        [Fact]
        public async Task Cloture_exceptionnelle_reservee_a_l_admin_avec_motif_et_stock_regularise()
        {
            var commandeId = CreerCommande(StatutCommande.StockReserve);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => CloturerAsync(commandeId, Roles.Commercial, "Motif"));
            await Assert.ThrowsAsync<ConflictException>(() => CloturerAsync(commandeId, Roles.Admin, null));
            // Stock encore réservé pour la commande
            await Assert.ThrowsAsync<ConflictException>(() => CloturerAsync(commandeId, Roles.Admin, "Doublon"));

            var sansStock = CreerCommande(StatutCommande.PaiementEchoue, payee: false);
            await CloturerAsync(sansStock, Roles.Admin, "Client injoignable depuis 3 mois");
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync(sansStock)).Statut);
        }

        // ---------- Utilitaires ----------

        /// <summary>
        /// Commande de 2 PC (600 000). Si payée : paiement confirmé ; à partir de StockReserve,
        /// 2 unités réservées (physique 5, réservé 2).
        /// </summary>
        private Guid CreerCommande(StatutCommande statut, bool payee = true)
        {
            var commande = new Commande
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("CMD"),
                Statut = statut,
                ClientId = _clientId,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+22890000000" }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = Montant, Total = Montant };
            version.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _produitId, Quantite = 2, PrixUnitaire = 300_000m, Total = Montant });
            commande.Versions.Add(version);
            _context.Commandes.Add(commande);

            if (payee)
            {
                _context.Paiements.Add(new Paiement
                {
                    Id = Guid.NewGuid(), Reference = $"PAY-{Guid.NewGuid():N}"[..20], Montant = Montant, Statut = StatutPaiement.Confirme,
                    Mode = ModePaiement.Externe, CommandeId = commande.Id, VersionCommandeId = version.Id
                });
            }

            if (statut == StatutCommande.StockReserve)
            {
                _context.StocksProduit.Single(s => s.Id == _stockId).QuantiteReservee += 2;
                _context.MouvementsStock.Add(new MouvementStock
                {
                    Id = Guid.NewGuid(), Type = TypeMouvementStock.Reservation, Quantite = 2, Reference = commande.Reference,
                    CommandeId = commande.Id, StockProduitId = _stockId
                });
            }

            _context.SaveChanges();
            return commande.Id;
        }

        private Task<AnnulerCommandeResult> AnnulerAsync(Guid commandeId, bool estClient, ModeRegularisation mode = ModeRegularisation.Remboursement)
        {
            if (estClient)
            {
                _user.UtilisateurId = _clientUtilisateurId;
                _user.Roles = new[] { Roles.Client };
            }
            else
            {
                _user.UtilisateurId = Guid.NewGuid();
                _user.Roles = new[] { Roles.Commercial };
            }
            return new AnnulerCommandeCommandHandler(_context, _user, _audit)
                .Handle(new AnnulerCommandeCommand { Id = commandeId, Motif = "Changement d'avis", Regularisation = mode }, default);
        }

        private async Task PreparerAsync(Guid commandeId)
        {
            ConnecterGestionnaire();
            var handler = new PreparationCommandsHandler(_context, _audit);
            await handler.Handle(new DemarrerPreparationCommand(commandeId), default);
            await handler.Handle(new TerminerPreparationCommand(commandeId), default);
        }

        private Task<Guid> PlanifierAsync(Guid commandeId)
        {
            ConnecterGestionnaire();
            return new PlanifierLivraisonCommandHandler(_context, _audit).Handle(new PlanifierLivraisonCommand
            {
                CommandeId = commandeId, LivreurId = _livreurId, DatePlanifiee = DateTime.UtcNow.AddDays(1)
            }, default);
        }

        private Task RemettreAsync(Guid livraisonId)
        {
            ConnecterGestionnaire();
            return new RemettreAuLivreurCommandHandler(_context, _user, _audit).Handle(new RemettreAuLivreurCommand(livraisonId), default);
        }

        private Task<List<object>> ReceptionnerAsync(Guid commandeId, int defectueux)
        {
            ConnecterGestionnaire();
            return new ReceptionnerRetourCommandHandler(_context, _audit).Handle(new ReceptionnerRetourCommand
            {
                CommandeId = commandeId,
                Defectueux = new() { new ProduitDefectueuxInput(_produitId, defectueux) }
            }, default);
        }

        private Task CloturerAsync(Guid commandeId, string role, string? motif = null)
        {
            _user.UtilisateurId = role == Roles.Admin ? _adminId : Guid.NewGuid();
            _user.Roles = new[] { role };
            return new CloturerCommandeCommandHandler(_context, _user, _audit).Handle(new CloturerCommandeCommand { Id = commandeId, Motif = motif }, default);
        }

        private async Task AssertStockAsync(int physique, int reserve, int transit, int defectueux)
        {
            var stock = await _context.StocksProduit.SingleAsync(s => s.Id == _stockId);
            Assert.Equal(physique, stock.QuantitePhysique);
            Assert.Equal(reserve, stock.QuantiteReservee);
            Assert.Equal(transit, stock.QuantiteEnTransit);
            Assert.Equal(defectueux, stock.QuantiteDefectueuse);
        }

        private Task<Commande> CommandeAsync(Guid id) => _context.Commandes.SingleAsync(c => c.Id == id);

        private void ConnecterAdmin()
        {
            _user.UtilisateurId = _adminId;
            _user.Roles = new[] { Roles.Admin };
        }

        private void ConnecterGestionnaire()
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.GestionnaireStock };
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
