using DigitalAllianceTogo.Application.Commandes.Commands.ExpirerCommandesImpayees;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Paiements.Commands.ConfirmerPaiement;
using DigitalAllianceTogo.Application.Paiements.Commands.RejeterPaiement;
using DigitalAllianceTogo.Application.Paiements.Commands.SoumettrePaiement;
using DigitalAllianceTogo.Application.Stock.Commands.EntreeStock;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Paiements
{
    /// <summary>
    /// Paiement externe → confirmation → réservation du stock (cahier des charges §9 et §11).
    /// </summary>
    public class PaiementReservationTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;

        private readonly Guid _pcId = Guid.NewGuid();      // 2 commandés par défaut
        private readonly Guid _sourisId = Guid.NewGuid();  // 1 commandée par défaut
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _clientUtilisateurId = Guid.NewGuid();
        private readonly Guid _commercialId = Guid.NewGuid();
        private readonly Guid _entrepotLomeId = Guid.NewGuid();
        private readonly Guid _entrepotKaraId = Guid.NewGuid();

        public PaiementReservationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated(); // ParametresEntreprise par défaut : délai de 48 h
            _audit = new AuditService(_context, _user);

            var categorie = new Categorie { Id = Guid.NewGuid(), Nom = "Informatique" };
            var marque = new Marque { Id = Guid.NewGuid(), Nom = "HP" };
            _context.Categories.Add(categorie);
            _context.Marques.Add(marque);
            _context.Produits.AddRange(
                new Produit { Id = _pcId, Reference = "HP-250", Nom = "HP 250 G9", Prix = 300_000m, CategorieId = categorie.Id, MarqueId = marque.Id },
                new Produit { Id = _sourisId, Reference = "SOURIS", Nom = "Souris", Prix = 5_000m, CategorieId = categorie.Id, MarqueId = marque.Id });
            _context.Utilisateurs.Add(new Utilisateur { Id = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client
            {
                Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _clientUtilisateurId,
                Nom = "Koffi", Prenom = "Ama", Telephone = "+22890000000"
            });
            _context.Entrepots.AddRange(
                new Entrepot { Id = _entrepotLomeId, Nom = "Lomé", Adresse = "Lomé" },
                new Entrepot { Id = _entrepotKaraId, Nom = "Kara", Adresse = "Kara" });
            _context.SaveChanges();
        }

        // ---------- Soumission de la preuve ----------

        [Fact]
        public async Task Soumettre_cree_un_paiement_en_attente_du_montant_de_la_version_active()
        {
            var commandeId = CreerCommande();

            var paiementId = await SoumettreAsync(commandeId, "TMONEY-123");

            var paiement = await _context.Paiements.SingleAsync(p => p.Id == paiementId);
            Assert.Equal(StatutPaiement.EnAttente, paiement.Statut);
            Assert.Equal(ModePaiement.Externe, paiement.Mode);
            Assert.Equal(605_000m, paiement.Montant);
            Assert.Equal("TMONEY-123", paiement.ReferenceExterne);
            Assert.Equal(StatutCommande.PaiementEnAttente, (await CommandeAsync(commandeId)).Statut);
            // Aucune réservation avant confirmation
            Assert.False(await _context.MouvementsStock.AnyAsync());
        }

        [Fact]
        public async Task Un_client_ne_peut_pas_payer_la_commande_d_un_autre()
        {
            var commandeId = CreerCommande();
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Client };

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new SoumettrePaiementCommandHandler(_context, _user, _audit)
                    .Handle(new SoumettrePaiementCommand { CommandeId = commandeId, ReferenceExterne = "X" }, default));
        }

        [Fact]
        public async Task Une_reference_de_transaction_ne_sert_qu_une_fois()
        {
            await SoumettreAsync(CreerCommande(), "flooz-777");

            // Même transaction (casse différente) pour une autre commande
            await Assert.ThrowsAsync<ConflictException>(() => SoumettreAsync(CreerCommande(), " FLOOZ-777 "));
        }

        [Fact]
        public async Task Pas_de_deuxieme_preuve_tant_que_la_premiere_n_est_pas_verifiee()
        {
            var commandeId = CreerCommande();
            await SoumettreAsync(commandeId, "REF-1");

            await Assert.ThrowsAsync<ConflictException>(() => SoumettreAsync(commandeId, "REF-2"));
        }

        // ---------- Confirmation et réservation ----------

        [Fact]
        public async Task Confirmer_reserve_tout_le_stock_et_trace_le_commercial()
        {
            AjouterStock(_pcId, _entrepotLomeId, 5);
            AjouterStock(_sourisId, _entrepotLomeId, 10);
            var commandeId = CreerCommande();
            var paiementId = await SoumettreAsync(commandeId, "REF-OK");

            var result = await ConfirmerAsync(paiementId);

            Assert.True(result.StockReserve);
            Assert.Equal(StatutCommande.StockReserve, (await CommandeAsync(commandeId)).Statut);

            var paiement = await _context.Paiements.SingleAsync(p => p.Id == paiementId);
            Assert.Equal(StatutPaiement.Confirme, paiement.Statut);
            Assert.Equal(_commercialId, paiement.ConfirmeParId);
            Assert.NotNull(paiement.DateConfirmation);

            var pc = await StockAsync(_pcId, _entrepotLomeId);
            Assert.Equal(5, pc.QuantitePhysique); // le physique ne bouge pas avant la remise au livreur
            Assert.Equal(2, pc.QuantiteReservee);
            Assert.Equal(3, pc.QuantiteDisponible);

            var mouvements = await _context.MouvementsStock.Where(m => m.CommandeId == commandeId).ToListAsync();
            Assert.Equal(2, mouvements.Count);
            Assert.All(mouvements, m => Assert.Equal(TypeMouvementStock.Reservation, m.Type));
            Assert.True(await _context.JournauxAudit.AnyAsync(j => j.EntiteId == commandeId && j.Action == "ReservationStock"));
        }

        [Fact]
        public async Task Reservation_tout_ou_rien_si_un_produit_manque()
        {
            AjouterStock(_pcId, _entrepotLomeId, 5);
            // Pas de souris en stock
            var commandeId = CreerCommande();

            var result = await ConfirmerAsync(await SoumettreAsync(commandeId, "REF-A"));

            Assert.False(result.StockReserve);
            Assert.Equal(StatutCommande.EnAttenteDisponibilite, (await CommandeAsync(commandeId)).Statut);
            // Les PC disponibles ne sont PAS réservés partiellement
            Assert.Equal(0, (await StockAsync(_pcId, _entrepotLomeId)).QuantiteReservee);
            Assert.False(await _context.MouvementsStock.AnyAsync(m => m.CommandeId == commandeId));
        }

        [Fact]
        public async Task Une_ligne_peut_etre_servie_par_plusieurs_entrepots()
        {
            AjouterStock(_pcId, _entrepotLomeId, 1);
            AjouterStock(_pcId, _entrepotKaraId, 1);
            AjouterStock(_sourisId, _entrepotKaraId, 1);
            var commandeId = CreerCommande();

            var result = await ConfirmerAsync(await SoumettreAsync(commandeId, "REF-B"));

            Assert.True(result.StockReserve);
            Assert.Equal(1, (await StockAsync(_pcId, _entrepotLomeId)).QuantiteReservee);
            Assert.Equal(1, (await StockAsync(_pcId, _entrepotKaraId)).QuantiteReservee);
        }

        [Fact]
        public async Task Le_stock_deja_reserve_n_est_pas_revendu()
        {
            AjouterStock(_pcId, _entrepotLomeId, 2);
            AjouterStock(_sourisId, _entrepotLomeId, 2);
            var premiere = CreerCommande();
            var seconde = CreerCommande();
            var paiement1 = await SoumettreAsync(premiere, "REF-1");
            var paiement2 = await SoumettreAsync(seconde, "REF-2");

            Assert.True((await ConfirmerAsync(paiement1)).StockReserve);
            Assert.False((await ConfirmerAsync(paiement2)).StockReserve);
            Assert.Equal(StatutCommande.EnAttenteDisponibilite, (await CommandeAsync(seconde)).Statut);
        }

        [Fact]
        public async Task Un_paiement_ne_peut_pas_etre_confirme_deux_fois()
        {
            AjouterStock(_pcId, _entrepotLomeId, 5);
            AjouterStock(_sourisId, _entrepotLomeId, 5);
            var paiementId = await SoumettreAsync(CreerCommande(), "REF-C");
            await ConfirmerAsync(paiementId);

            await Assert.ThrowsAsync<ConflictException>(() => ConfirmerAsync(paiementId));
            Assert.Equal(2, (await StockAsync(_pcId, _entrepotLomeId)).QuantiteReservee);
        }

        [Fact]
        public async Task Entree_de_stock_relance_les_commandes_en_attente_premier_paye_premier_servi()
        {
            AjouterStock(_sourisId, _entrepotLomeId, 10);
            var premiere = CreerCommande();
            var seconde = CreerCommande();
            await ConfirmerAsync(await SoumettreAsync(premiere, "REF-1"));
            await ConfirmerAsync(await SoumettreAsync(seconde, "REF-2"));

            // Seulement 2 PC reçus : de quoi servir UNE commande, la première payée
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.GestionnaireStock };
            var result = await new EntreeStockCommandHandler(_context, _user, _audit).Handle(new EntreeStockCommand
            {
                ProduitId = _pcId, EntrepotId = _entrepotLomeId, Quantite = 2, Reference = "BL-001"
            }, default);

            Assert.Equal(new[] { (await CommandeAsync(premiere)).Reference }, result.CommandesReservees);
            Assert.Equal(StatutCommande.StockReserve, (await CommandeAsync(premiere)).Statut);
            Assert.Equal(StatutCommande.EnAttenteDisponibilite, (await CommandeAsync(seconde)).Statut);
            Assert.Equal(0, result.QuantiteDisponible);
            Assert.True(await _context.MouvementsStock.AnyAsync(m => m.Type == TypeMouvementStock.Entree && m.Reference == "BL-001"));
        }

        // ---------- Rejet et nouvelle tentative ----------

        [Fact]
        public async Task Rejet_puis_nouvelle_preuve_dans_le_delai()
        {
            var commandeId = CreerCommande();
            var paiementId = await SoumettreAsync(commandeId, "REF-FAUSSE");

            ConnecterCommercial();
            await new RejeterPaiementCommandHandler(_context, _user, _audit)
                .Handle(new RejeterPaiementCommand { Id = paiementId, Motif = "Transaction introuvable" }, default);

            var paiement = await _context.Paiements.SingleAsync(p => p.Id == paiementId);
            Assert.Equal(StatutPaiement.Echoue, paiement.Statut);
            Assert.Equal("Transaction introuvable", paiement.MotifRejet);
            Assert.Equal(_commercialId, paiement.ConfirmeParId);
            Assert.Equal(StatutCommande.PaiementEchoue, (await CommandeAsync(commandeId)).Statut);

            // Le client réessaie ; la référence rejetée peut même être corrigée et resoumise
            await SoumettreAsync(commandeId, "REF-FAUSSE");
            Assert.Equal(StatutCommande.PaiementEnAttente, (await CommandeAsync(commandeId)).Statut);
        }

        // ---------- Expiration ----------

        [Fact]
        public async Task Expiration_annule_les_commandes_impayees_mais_pas_celles_en_verification()
        {
            var jamaisPayee = CreerCommande(creeIlYA: TimeSpan.FromHours(49));
            var recente = CreerCommande(creeIlYA: TimeSpan.FromHours(2));
            var enVerification = CreerCommande(creeIlYA: TimeSpan.FromHours(49));
            await SoumettreAsync(enVerification, "REF-V", verifierDelai: false);

            _user.UtilisateurId = null; // tâche planifiée : aucun utilisateur
            _user.Roles = Array.Empty<string>();
            var annulees = await new ExpirerCommandesImpayeesCommandHandler(_context, _audit)
                .Handle(new ExpirerCommandesImpayeesCommand(), default);

            Assert.Equal(1, annulees);
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync(jamaisPayee)).Statut); // Annulée puis Clôturée (§19)
            Assert.Equal(StatutCommande.CommandeCreee, (await CommandeAsync(recente)).Statut);
            Assert.Equal(StatutCommande.PaiementEnAttente, (await CommandeAsync(enVerification)).Statut);

            var journal = await _context.JournauxAudit.SingleAsync(j => j.EntiteId == jamaisPayee && j.Action == "ExpirationCommande");
            Assert.Null(journal.UtilisateurId); // action du système
        }

        [Fact]
        public async Task Le_delai_repart_du_dernier_rejet()
        {
            var commandeId = CreerCommande(creeIlYA: TimeSpan.FromHours(100));
            var paiementId = await SoumettreAsync(commandeId, "REF-X", verifierDelai: false);
            ConnecterCommercial();
            await new RejeterPaiementCommandHandler(_context, _user, _audit)
                .Handle(new RejeterPaiementCommand { Id = paiementId, Motif = "Montant incorrect" }, default);

            var annulees = await new ExpirerCommandesImpayeesCommandHandler(_context, _audit)
                .Handle(new ExpirerCommandesImpayeesCommand(), default);

            Assert.Equal(0, annulees); // rejet d'il y a quelques secondes : le client a encore 48 h
        }

        [Fact]
        public async Task Payer_apres_le_delai_annule_la_commande()
        {
            var commandeId = CreerCommande(creeIlYA: TimeSpan.FromHours(49));

            await Assert.ThrowsAsync<ConflictException>(() => SoumettreAsync(commandeId, "REF-TARD"));
            Assert.Equal(StatutCommande.Cloturee, (await CommandeAsync(commandeId)).Statut);
        }

        // ---------- Utilitaires ----------

        /// <summary>Commande de 2 PC (300 000) + 1 souris (5 000) = 605 000.</summary>
        private Guid CreerCommande(TimeSpan? creeIlYA = null)
        {
            var commande = new Commande
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("CMD"),
                Statut = StatutCommande.CommandeCreee,
                DateCreation = DateTime.UtcNow - (creeIlYA ?? TimeSpan.Zero),
                ClientId = _clientId,
                AdresseLivraison = new AdresseLivraisonCommande
                {
                    Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+22890000000"
                }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = 605_000m, Total = 605_000m };
            version.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _pcId, Quantite = 2, PrixUnitaire = 300_000m, Total = 600_000m });
            version.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _sourisId, Quantite = 1, PrixUnitaire = 5_000m, Total = 5_000m });
            commande.Versions.Add(version);
            _context.Commandes.Add(commande);
            _context.SaveChanges();
            return commande.Id;
        }

        private void AjouterStock(Guid produitId, Guid entrepotId, int quantite)
        {
            _context.StocksProduit.Add(new StockProduit
            {
                Id = Guid.NewGuid(), ProduitId = produitId, EntrepotId = entrepotId, QuantitePhysique = quantite
            });
            _context.SaveChanges();
        }

        private async Task<Guid> SoumettreAsync(Guid commandeId, string referenceExterne, bool verifierDelai = true)
        {
            _user.UtilisateurId = _clientUtilisateurId;
            _user.Roles = new[] { Roles.Client };

            if (!verifierDelai)
            {
                // Soumission « à l'époque » : on neutralise temporairement le délai
                var parametres = await _context.ParametresEntreprise.SingleAsync();
                var delai = parametres.DelaiExpirationPaiementHeures;
                parametres.DelaiExpirationPaiementHeures = 10_000;
                await _context.SaveChangesAsync();
                try
                {
                    return await SoumettreAsync(commandeId, referenceExterne);
                }
                finally
                {
                    parametres.DelaiExpirationPaiementHeures = delai;
                    await _context.SaveChangesAsync();
                }
            }

            return await new SoumettrePaiementCommandHandler(_context, _user, _audit)
                .Handle(new SoumettrePaiementCommand { CommandeId = commandeId, ReferenceExterne = referenceExterne }, default);
        }

        private Task<ConfirmerPaiementResult> ConfirmerAsync(Guid paiementId)
        {
            ConnecterCommercial();
            return new ConfirmerPaiementCommandHandler(_context, _user, _audit)
                .Handle(new ConfirmerPaiementCommand(paiementId), default);
        }

        private void ConnecterCommercial()
        {
            _user.UtilisateurId = _commercialId;
            _user.Roles = new[] { Roles.Commercial };
        }

        private Task<Commande> CommandeAsync(Guid id) => _context.Commandes.SingleAsync(c => c.Id == id);

        private Task<StockProduit> StockAsync(Guid produitId, Guid entrepotId) =>
            _context.StocksProduit.SingleAsync(s => s.ProduitId == produitId && s.EntrepotId == entrepotId);

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
