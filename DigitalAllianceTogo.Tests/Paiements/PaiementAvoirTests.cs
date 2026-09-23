using DigitalAllianceTogo.Application.Commandes.Commands.AnnulerCommande;
using DigitalAllianceTogo.Application.Commandes.Commands.ExpirerCommandesImpayees;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Finance.Commands;
using DigitalAllianceTogo.Application.Paiements.Commands.ConfirmerPaiement;
using DigitalAllianceTogo.Application.Paiements.Commands.PayerAvecAvoir;
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

namespace DigitalAllianceTogo.Tests.Paiements
{
    /// <summary>Paiement avec un avoir (§23) et « reste à payer ».</summary>
    public class PaiementAvoirTests
    {
        private const decimal Total = 600_000m; // 2 PC à 300 000

        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;
        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _stockId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _clientUtilisateurId = Guid.NewGuid();

        public PaiementAvoirTests()
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
            _context.Utilisateurs.Add(new Utilisateur { Id = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _clientUtilisateurId, Nom = "Koffi", Telephone = "+228" });
            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = "Lomé", Adresse = "Lomé" };
            _context.Entrepots.Add(entrepot);
            _context.StocksProduit.Add(new StockProduit { Id = _stockId, ProduitId = _produitId, EntrepotId = entrepot.Id, QuantitePhysique = 10 });
            _context.SaveChanges();
        }

        [Fact]
        public async Task Avoir_qui_couvre_tout_confirme_le_paiement_et_reserve_le_stock()
        {
            var avoirId = CreerAvoir(700_000m);
            var commandeId = CreerCommande();

            var result = await PayerAvecAvoirAsync(commandeId, avoirId);

            Assert.Equal(Total, result.MontantUtilise);
            Assert.Equal(0, result.ResteAPayer);
            Assert.Equal(100_000m, result.SoldeAvoir);
            Assert.Equal(StatutCommande.StockReserve.ToString(), result.StatutCommande);
            Assert.Equal(2, (await _context.StocksProduit.SingleAsync(s => s.Id == _stockId)).QuantiteReservee);

            var avoir = await _context.Avoirs.SingleAsync(a => a.Id == avoirId);
            Assert.Equal(StatutAvoir.Disponible, avoir.Statut); // solde de 100 000 encore utilisable
            var paiement = await _context.Paiements.SingleAsync(p => p.CommandeId == commandeId);
            Assert.Equal(ModePaiement.Avoir, paiement.Mode);
            Assert.Equal(StatutPaiement.Confirme, paiement.Statut);
            Assert.Equal(avoirId, paiement.AvoirId);
        }

        [Fact]
        public async Task Avoir_partiel_puis_reste_paye_par_mobile_money()
        {
            var avoirId = CreerAvoir(100_000m);
            var commandeId = CreerCommande();

            var result = await PayerAvecAvoirAsync(commandeId, avoirId);

            Assert.Equal(500_000m, result.ResteAPayer);
            Assert.Equal(StatutCommande.CommandeCreee.ToString(), result.StatutCommande);
            Assert.Equal(StatutAvoir.Utilise, (await _context.Avoirs.SingleAsync(a => a.Id == avoirId)).Statut);

            // Le paiement externe ne demande que le reste
            ConnecterClient();
            var paiementId = await new SoumettrePaiementCommandHandler(_context, _user, _audit)
                .Handle(new SoumettrePaiementCommand { CommandeId = commandeId, ReferenceExterne = "TMONEY-RESTE" }, default);
            Assert.Equal(500_000m, (await _context.Paiements.SingleAsync(p => p.Id == paiementId)).Montant);

            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Commercial };
            var confirmation = await new ConfirmerPaiementCommandHandler(_context, _user, _audit).Handle(new ConfirmerPaiementCommand(paiementId), default);
            Assert.True(confirmation.StockReserve);
        }

        [Fact]
        public async Task Un_avoir_n_est_utilisable_que_par_son_client_et_une_fois_disponible()
        {
            var commandeId = CreerCommande();

            var avoirEnAttente = CreerAvoir(100_000m, StatutAvoir.EnAttente);
            await Assert.ThrowsAsync<ConflictException>(() => PayerAvecAvoirAsync(commandeId, avoirEnAttente));

            var autreClient = new Client { Id = Guid.NewGuid(), CodeClient = "CLI-002", Nom = "Autre", Telephone = "+228" };
            _context.Clients.Add(autreClient);
            _context.SaveChanges();
            var avoirAutreClient = CreerAvoir(100_000m, clientId: autreClient.Id);
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => PayerAvecAvoirAsync(commandeId, avoirAutreClient));
        }

        [Fact]
        public async Task Un_avoir_deja_entame_ne_peut_plus_etre_annule()
        {
            var avoirId = CreerAvoir(700_000m);
            await PayerAvecAvoirAsync(CreerCommande(), avoirId);

            _user.Roles = new[] { Roles.Admin };
            await Assert.ThrowsAsync<ConflictException>(() => new AvoirCommandsHandler(_context, _audit)
                .Handle(new AnnulerAvoirCommand { Id = avoirId, Motif = "Erreur" }, default));
        }

        [Fact]
        public async Task Commande_en_partie_payee_ni_expiree_ni_cloturee_sans_rendre_l_argent()
        {
            var avoirId = CreerAvoir(100_000m);
            var commandeId = CreerCommande(creeIlYA: TimeSpan.FromDays(10));
            await PayerAvecAvoirAsync(commandeId, avoirId);

            // La tâche d'expiration l'ignore
            var annulees = await new ExpirerCommandesImpayeesCommandHandler(_context, _audit).Handle(new ExpirerCommandesImpayeesCommand(), default);
            Assert.Equal(0, annulees);

            // Annulée par le client : les 100 000 déjà versés sont à régulariser
            ConnecterClient();
            var result = await new AnnulerCommandeCommandHandler(_context, _user, _audit)
                .Handle(new AnnulerCommandeCommand { Id = commandeId, Motif = "Changement d'avis", Regularisation = ModeRegularisation.Avoir }, default);

            Assert.Equal(StatutCommande.EnAttenteRegulationFinanciere.ToString(), result.Statut);
            Assert.Equal(100_000m, result.MontantARegulariser);
        }

        // ---------- Utilitaires ----------

        private Guid CreerCommande(TimeSpan? creeIlYA = null)
        {
            var commande = new Commande
            {
                Id = Guid.NewGuid(), Reference = $"CMD-{Guid.NewGuid():N}"[..20], Statut = StatutCommande.CommandeCreee, ClientId = _clientId,
                DateCreation = DateTime.UtcNow - (creeIlYA ?? TimeSpan.Zero),
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+228" }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = Total, Total = Total };
            version.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _produitId, Quantite = 2, PrixUnitaire = 300_000m, Total = Total });
            commande.Versions.Add(version);
            _context.Commandes.Add(commande);
            _context.SaveChanges();
            return commande.Id;
        }

        /// <summary>Avoir né d'une ancienne commande annulée (clôturée) du client.</summary>
        private Guid CreerAvoir(decimal montant, StatutAvoir statut = StatutAvoir.Disponible, Guid? clientId = null)
        {
            var ancienne = new Commande
            {
                Id = Guid.NewGuid(), Reference = $"CMD-{Guid.NewGuid():N}"[..20], Statut = StatutCommande.Cloturee, ClientId = clientId ?? _clientId,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+228" }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, Total = montant };
            ancienne.Versions.Add(version);
            _context.Commandes.Add(ancienne);
            var avoir = new Avoir
            {
                Id = Guid.NewGuid(), Reference = $"AVO-{Guid.NewGuid():N}"[..20], Montant = montant, Statut = statut, Motif = "Annulation",
                CommandeId = ancienne.Id, VersionCommandeId = version.Id
            };
            _context.Avoirs.Add(avoir);
            _context.SaveChanges();
            return avoir.Id;
        }

        private Task<PayerAvecAvoirResult> PayerAvecAvoirAsync(Guid commandeId, Guid avoirId)
        {
            ConnecterClient();
            return new PayerAvecAvoirCommandHandler(_context, _user, _audit)
                .Handle(new PayerAvecAvoirCommand { CommandeId = commandeId, AvoirId = avoirId }, default);
        }

        private void ConnecterClient()
        {
            _user.UtilisateurId = _clientUtilisateurId;
            _user.Roles = new[] { Roles.Client };
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
