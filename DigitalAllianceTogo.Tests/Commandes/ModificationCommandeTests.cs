using DigitalAllianceTogo.Application.Commandes.Commands.AnnulerCommande;
using DigitalAllianceTogo.Application.Commandes.Commands.Modification;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Application.Paiements.Commands.ConfirmerPaiement;
using DigitalAllianceTogo.Application.Paiements.Commands.SoumettrePaiement;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Finance;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Commandes
{
    /// <summary>
    /// Modification d'une commande payée (§20-21) : nouvelle version, ancienne intacte,
    /// validation Admin hors seuil, complément à payer ou trop-perçu à rendre.
    /// </summary>
    public class ModificationCommandeTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;

        private readonly Guid _pcId = Guid.NewGuid();      // 300 000
        private readonly Guid _sourisId = Guid.NewGuid();  // 5 000
        private readonly Guid _stockPcId = Guid.NewGuid();
        private readonly Guid _stockSourisId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _clientUtilisateurId = Guid.NewGuid();
        private readonly Guid _commandeId = Guid.NewGuid();
        private readonly Guid _v1Id = Guid.NewGuid();
        private readonly Guid _paiementV1Id = Guid.NewGuid();

        public ModificationCommandeTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated(); // seuils par défaut : 10 % (hausse) et 10 % (remise)
            _audit = new AuditService(_context, _user);

            var categorie = new Categorie { Id = Guid.NewGuid(), Nom = "Informatique" };
            var marque = new Marque { Id = Guid.NewGuid(), Nom = "HP" };
            _context.Categories.Add(categorie);
            _context.Marques.Add(marque);
            _context.Produits.AddRange(
                new Produit { Id = _pcId, Reference = "HP-250", Nom = "HP 250 G9", Prix = 300_000m, CategorieId = categorie.Id, MarqueId = marque.Id },
                new Produit { Id = _sourisId, Reference = "SOURIS", Nom = "Souris", Prix = 5_000m, CategorieId = categorie.Id, MarqueId = marque.Id });
            _context.Utilisateurs.Add(new Utilisateur { Id = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _clientUtilisateurId, Nom = "Koffi", Telephone = "+228" });

            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = "Lomé", Adresse = "Lomé" };
            _context.Entrepots.Add(entrepot);
            _context.StocksProduit.AddRange(
                new StockProduit { Id = _stockPcId, ProduitId = _pcId, EntrepotId = entrepot.Id, QuantitePhysique = 10, QuantiteReservee = 2 },
                new StockProduit { Id = _stockSourisId, ProduitId = _sourisId, EntrepotId = entrepot.Id, QuantitePhysique = 10 });

            // Commande #100 : v1 = 2 PC = 600 000, payée, stock réservé
            var commande = new Commande
            {
                Id = _commandeId, Reference = "CMD-100", Statut = StatutCommande.StockReserve, ClientId = _clientId, VersionActive = 1,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+228" }
            };
            var v1 = new VersionCommande { Id = _v1Id, NumeroVersion = 1, SousTotal = 600_000m, Total = 600_000m, Active = true };
            v1.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _pcId, Quantite = 2, PrixUnitaire = 300_000m, Total = 600_000m });
            commande.Versions.Add(v1);
            _context.Commandes.Add(commande);
            _context.Paiements.Add(new Paiement
            {
                Id = _paiementV1Id, Reference = "PAY-V1", Montant = 600_000m, Statut = StatutPaiement.Confirme, Mode = ModePaiement.Externe,
                DateConfirmation = DateTime.UtcNow, CommandeId = _commandeId, VersionCommandeId = _v1Id
            });
            _context.MouvementsStock.Add(new MouvementStock
            {
                Id = Guid.NewGuid(), Type = TypeMouvementStock.Reservation, Quantite = 2, Reference = "CMD-100", CommandeId = _commandeId, StockProduitId = _stockPcId
            });
            _context.SaveChanges();
        }

        // ---------- Même prix ----------

        [Fact]
        public async Task Meme_prix_commercial_et_client_suffisent_ancienne_version_intacte()
        {
            // 2 PC + 1 souris offerte (remise de ligne) : toujours 600 000
            var proposition = await ProposerAsync(Ligne(_pcId, 2), Ligne(_sourisId, 1, remise: 5_000));
            Assert.Equal(StatutVersionCommande.EnAttenteClient.ToString(), proposition.Statut);
            Assert.Equal(0, proposition.Ecart);

            var reponse = await RepondreAsync(accepter: true);

            Assert.Equal(2, reponse.VersionActive);
            Assert.Equal(0, reponse.ResteAPayer);
            Assert.Equal(StatutCommande.StockReserve.ToString(), reponse.StatutCommande);

            // v1 : intacte, conservée, plus active ; son paiement lui reste attaché
            var v1 = await _context.VersionsCommande.Include(v => v.Lignes).SingleAsync(v => v.Id == _v1Id);
            Assert.False(v1.Active);
            Assert.Equal(600_000m, v1.Total);
            Assert.Single(v1.Lignes);
            Assert.Equal(_v1Id, (await _context.Paiements.SingleAsync(p => p.Id == _paiementV1Id)).VersionCommandeId);

            // Stock : PC toujours réservés, souris réservée en plus
            await AssertReserveAsync(pc: 2, souris: 1);
        }

        // ---------- Hausse ----------

        [Fact]
        public async Task Hausse_sous_le_seuil_complement_a_payer_avant_de_poursuivre()
        {
            await ProposerAsync(Ligne(_pcId, 2), Ligne(_sourisId, 1)); // +5 000 (0,8 %)

            var reponse = await RepondreAsync(accepter: true);

            Assert.Equal(5_000m, reponse.ResteAPayer);
            Assert.Equal(StatutCommande.CommandeCreee.ToString(), reponse.StatutCommande);
            await AssertReserveAsync(pc: 2, souris: 0); // les PC restent réservés, la souris attend le complément

            // Le client paie le complément, lié à la version 2
            ConnecterClient();
            var paiementId = await new SoumettrePaiementCommandHandler(_context, _user, _audit)
                .Handle(new SoumettrePaiementCommand { CommandeId = _commandeId, ReferenceExterne = "TM-COMPLEMENT" }, default);
            var complement = await _context.Paiements.Include(p => p.VersionCommande).SingleAsync(p => p.Id == paiementId);
            Assert.Equal(5_000m, complement.Montant);
            Assert.Equal(2, complement.VersionCommande.NumeroVersion);

            ConnecterCommercial();
            var confirmation = await new ConfirmerPaiementCommandHandler(_context, _user, _audit).Handle(new ConfirmerPaiementCommand(paiementId), default);
            Assert.True(confirmation.StockReserve);
            await AssertReserveAsync(pc: 2, souris: 1);
        }

        [Fact]
        public async Task Hausse_au_dessus_du_seuil_validee_par_l_admin_avant_le_client()
        {
            var proposition = await ProposerAsync(Ligne(_pcId, 3)); // +300 000 (50 %)
            Assert.Equal(StatutVersionCommande.EnValidationAdmin.ToString(), proposition.Statut);

            await Assert.ThrowsAsync<ConflictException>(() => RepondreAsync(accepter: true));

            _user.Roles = new[] { Roles.Admin };
            await Handler().Handle(new DeciderModificationAdminCommand { CommandeId = _commandeId, Valider = true }, default);

            var reponse = await RepondreAsync(accepter: true);
            Assert.Equal(300_000m, reponse.ResteAPayer);
        }

        [Fact]
        public async Task Remise_exceptionnelle_du_commercial_soumise_a_l_admin()
        {
            var proposition = await ProposerAsync(Ligne(_pcId, 2, remise: 90_000)); // 15 % de remise

            Assert.Equal(StatutVersionCommande.EnValidationAdmin.ToString(), proposition.Statut);
        }

        // ---------- Baisse ----------

        [Fact]
        public async Task Baisse_de_prix_trop_percu_rendu_par_avoir_valide_par_l_admin()
        {
            await ProposerAsync(Ligne(_pcId, 1)); // 300 000 au lieu de 600 000

            var reponse = await RepondreAsync(accepter: true, ModeRegularisation.Avoir);

            Assert.Equal(300_000m, reponse.ARendre);
            Assert.Equal(StatutCommande.StockReserve.ToString(), reponse.StatutCommande);
            await AssertReserveAsync(pc: 1, souris: 0); // un PC libéré

            var avoir = await _context.Avoirs.Include(a => a.VersionCommande).SingleAsync();
            Assert.Equal(StatutAvoir.EnAttente, avoir.Statut); // le Commercial ne réalise pas l'opération
            Assert.Equal(300_000m, avoir.Montant);
            Assert.Equal(2, avoir.VersionCommande.NumeroVersion);
        }

        // ---------- Garde-fous ----------

        [Fact]
        public async Task Refus_du_client_la_commande_continue_avec_la_version_actuelle()
        {
            await ProposerAsync(Ligne(_pcId, 1));

            await RepondreAsync(accepter: false);

            Assert.Equal(1, (await _context.Commandes.SingleAsync()).VersionActive);
            Assert.Equal(StatutVersionCommande.Refusee, (await _context.VersionsCommande.SingleAsync(v => v.NumeroVersion == 2)).Statut);
            await AssertReserveAsync(pc: 2, souris: 0);
        }

        [Fact]
        public async Task Pas_de_modification_avant_paiement_ni_identique_ni_en_double()
        {
            await Assert.ThrowsAsync<ConflictException>(() => ProposerAsync(Ligne(_pcId, 2))); // identique

            await ProposerAsync(Ligne(_pcId, 1));
            await Assert.ThrowsAsync<ConflictException>(() => ProposerAsync(Ligne(_pcId, 3))); // une seule en cours

            (await _context.Commandes.SingleAsync()).Statut = StatutCommande.CommandeCreee;
            await _context.SaveChangesAsync();
            await Assert.ThrowsAsync<ConflictException>(() => RepondreAsync(accepter: true)); // plus modifiable
        }

        [Fact]
        public async Task Annuler_la_commande_rend_la_proposition_caduque()
        {
            await ProposerAsync(Ligne(_pcId, 1));

            ConnecterClient();
            await new AnnulerCommandeCommandHandler(_context, _user, _audit)
                .Handle(new AnnulerCommandeCommand { Id = _commandeId, Motif = "Plus besoin" }, default);

            Assert.Equal(StatutVersionCommande.Retiree, (await _context.VersionsCommande.SingleAsync(v => v.NumeroVersion == 2)).Statut);
        }

        [Fact]
        public async Task Commande_en_preparation_repasse_par_la_preparation()
        {
            (await _context.Commandes.SingleAsync()).Statut = StatutCommande.PreparationEnCours;
            await _context.SaveChangesAsync();
            await ProposerAsync(Ligne(_pcId, 2), Ligne(_sourisId, 1, remise: 5_000));

            var reponse = await RepondreAsync(accepter: true);

            Assert.Equal(StatutCommande.StockReserve.ToString(), reponse.StatutCommande);
        }

        // ---------- Utilitaires ----------

        private static LigneDevisInput Ligne(Guid produitId, int quantite, decimal remise = 0) =>
            new() { ProduitId = produitId, Quantite = quantite, Remise = remise };

        private Task<ProposerModificationResult> ProposerAsync(params LigneDevisInput[] lignes)
        {
            ConnecterCommercial();
            return new ProposerModificationCommandHandler(_context, _user, _audit).Handle(new ProposerModificationCommand
            {
                CommandeId = _commandeId, Lignes = lignes.ToList(), Motif = "Demande du client"
            }, default);
        }

        private Task<RepondreModificationResult> RepondreAsync(bool accepter, ModeRegularisation mode = ModeRegularisation.Remboursement)
        {
            ConnecterClient();
            return Handler().Handle(new RepondreModificationCommand { CommandeId = _commandeId, Accepter = accepter, Regularisation = mode }, default);
        }

        private ReponsesModificationCommandsHandler Handler() => new(_context, _user, _audit);

        private async Task AssertReserveAsync(int pc, int souris)
        {
            Assert.Equal(pc, (await _context.StocksProduit.SingleAsync(s => s.Id == _stockPcId)).QuantiteReservee);
            Assert.Equal(souris, (await _context.StocksProduit.SingleAsync(s => s.Id == _stockSourisId)).QuantiteReservee);
        }

        private void ConnecterClient()
        {
            _user.UtilisateurId = _clientUtilisateurId;
            _user.Roles = new[] { Roles.Client };
        }

        private void ConnecterCommercial()
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Commercial };
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
