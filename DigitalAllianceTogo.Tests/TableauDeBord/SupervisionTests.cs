using DigitalAllianceTogo.Application.Audit.Queries;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.TableauDeBord.Queries;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Audit;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Finance;
using DigitalAllianceTogo.Domain.Models.Livraison;
using DigitalAllianceTogo.Domain.Models.SAV;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using DevisEntity = DigitalAllianceTogo.Domain.Models.Devis.Devis;

namespace DigitalAllianceTogo.Tests.TableauDeBord
{
    /// <summary>Phase 7 : audit inviolable et consultable, supervision, statistiques.</summary>
    public class SupervisionTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;
        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _utilisateurId = Guid.NewGuid();

        public SupervisionTests()
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
            _context.Utilisateurs.Add(new Utilisateur { Id = _utilisateurId, Nom = "Mensah", Prenom = "Kodjo", Email = "kodjo@test.tg" });
            _context.SaveChanges();
        }

        // ---------- Audit inviolable (§45) ----------

        [Fact]
        public async Task Le_journal_d_audit_ne_peut_etre_ni_modifie_ni_supprime()
        {
            ConnecterAdmin();
            _audit.Enregistrer("Test", "Commande", Guid.NewGuid(), apres: new { Statut = "X" });
            await _context.SaveChangesAsync();

            var entree = await _context.JournauxAudit.SingleAsync();
            entree.Action = "Falsifie";
            await Assert.ThrowsAsync<InvalidOperationException>(() => _context.SaveChangesAsync());

            _context.Entry(entree).State = EntityState.Deleted;
            await Assert.ThrowsAsync<InvalidOperationException>(() => _context.SaveChangesAsync());
        }

        // ---------- Consultation ----------

        [Fact]
        public async Task Historique_d_une_commande_rassemble_toute_sa_vie_dans_l_ordre()
        {
            var (commande, devisId, paiementId) = CreerCommandeLivree(DateTime.UtcNow);
            var autreCommande = Guid.NewGuid();
            var t0 = DateTime.UtcNow.AddHours(-5);
            _context.JournauxAudit.AddRange(
                Journal("CreationDevis", "Devis", devisId, t0, _utilisateurId),
                Journal("CreationCommande", "Commande", commande.Id, t0.AddHours(1), _utilisateurId),
                Journal("ConfirmationPaiement", "Paiement", paiementId, t0.AddHours(2), _utilisateurId),
                Journal("ExpirationCommande", "Commande", autreCommande, t0.AddHours(3), null)); // autre commande
            await _context.SaveChangesAsync();

            var historique = await new AuditQueriesHandler(_context).Handle(new GetHistoriqueCommandeQuery(commande.Id), default);

            Assert.Equal(new[] { "CreationDevis", "CreationCommande", "ConfirmationPaiement" }, historique.Select(h => h.Action));
            Assert.All(historique, h => Assert.Equal("Kodjo Mensah", h.Auteur));
        }

        [Fact]
        public async Task Recherche_des_actions_automatiques_du_systeme()
        {
            _context.JournauxAudit.AddRange(
                Journal("ExpirationCommande", "Commande", Guid.NewGuid(), DateTime.UtcNow, null),
                Journal("ValidationDevisAdmin", "Devis", Guid.NewGuid(), DateTime.UtcNow, _utilisateurId));
            await _context.SaveChangesAsync();

            var resultat = await new AuditQueriesHandler(_context).Handle(new GetJournalAuditQuery { Systeme = true }, default);

            var ligne = Assert.Single(resultat.Items);
            Assert.Equal("ExpirationCommande", ligne.Action);
            Assert.Equal("Système", ligne.Auteur);
        }

        // ---------- Supervision ----------

        [Fact]
        public async Task Chaque_role_ne_voit_que_ses_files_l_admin_voit_tout()
        {
            var ancien = DateTime.UtcNow.AddDays(-3);
            var (commande, _, _) = CreerCommandeLivree(ancien);
            _context.Paiements.Add(new Paiement
            {
                Id = Guid.NewGuid(), Reference = "PAY-ATT", Montant = 1, Statut = StatutPaiement.EnAttente, Mode = ModePaiement.Externe,
                DatePaiement = ancien, CommandeId = commande.Id, VersionCommandeId = commande.Versions.First().Id
            });
            _context.TicketsSAV.Add(new TicketSAV
            {
                Id = Guid.NewGuid(), Reference = "SAV-1", Motif = "Panne", Statut = StatutSav.Ouvert, DateCreation = DateTime.UtcNow,
                ClientId = commande.ClientId, LigneCommandeId = commande.Versions.First().Lignes.First().Id
            });
            await _context.SaveChangesAsync();

            _user.Roles = new[] { Roles.Technicien };
            var technicien = await new GetATraiterQueryHandler(_context, _user).Handle(new GetATraiterQuery(), default);
            Assert.All(technicien, f => Assert.Equal(Roles.Technicien, f.Responsable));
            Assert.Equal(1, technicien.Single(f => f.Cle == "sav-a-diagnostiquer").Nombre);

            _user.Roles = new[] { Roles.Commercial };
            var paiements = (await new GetATraiterQueryHandler(_context, _user).Handle(new GetATraiterQuery(), default))
                .Single(f => f.Cle == "paiements-a-verifier");
            Assert.Equal(1, paiements.Nombre);
            Assert.Equal(ancien, paiements.PlusAncien);

            ConnecterAdmin();
            var admin = await new GetATraiterQueryHandler(_context, _user).Handle(new GetATraiterQuery(), default);
            Assert.Equal(4, admin.Select(f => f.Responsable).Distinct().Count());
        }

        // ---------- Statistiques ----------

        [Fact]
        public async Task Statistiques_ventes_devis_acquisition_et_top_produits()
        {
            var maintenant = DateTime.UtcNow;
            // 2 clients TikTok arrivés sur la période, dont 1 a payé ; 1 client WhatsApp qui a payé
            var (cmdTikTok, _, _) = CreerCommandeLivree(maintenant.AddDays(-2), SourceClient.TikTok, montant: 600_000m);
            CreerClient(SourceClient.TikTok, maintenant.AddDays(-1));
            CreerCommandeLivree(maintenant.AddDays(-3), SourceClient.WhatsApp, montant: 300_000m);
            // Paiement hors période : ignoré
            CreerCommandeLivree(maintenant.AddDays(-60), SourceClient.Facebook, montant: 999_000m);

            _context.Remboursements.Add(new Remboursement
            {
                Id = Guid.NewGuid(), Reference = "RBT-1", Montant = 100_000m, Statut = StatutRemboursement.Execute, Motif = "x",
                DateExecution = maintenant.AddDays(-1), CommandeId = cmdTikTok.Id, VersionCommandeId = cmdTikTok.Versions.First().Id
            });
            // 4 devis créés sur la période, 1 accepté ; remises 10 % et 0 %
            for (var i = 0; i < 4; i++)
                _context.Devis.Add(new DevisEntity
                {
                    Id = Guid.NewGuid(), Reference = $"DEV-{i}", ClientId = cmdTikTok.ClientId, DateCreation = maintenant.AddDays(-1),
                    Statut = i == 0 ? StatutDevis.Accepte : StatutDevis.Envoye, SousTotal = 100_000m, Remise = i < 2 ? 10_000m : 0, Total = 100_000m
                });
            await _context.SaveChangesAsync();

            var stats = await new GetStatistiquesQueryHandler(_context).Handle(new GetStatistiquesQuery(), default);

            Assert.Equal(2, stats.Ventes.PaiementsConfirmes);
            Assert.Equal(900_000m, stats.Ventes.Encaisse);
            Assert.Equal(100_000m, stats.Ventes.Rembourse);
            Assert.Equal(800_000m, stats.Ventes.EncaisseNet);
            Assert.Equal(450_000m, stats.Ventes.PanierMoyen);
            Assert.Equal(2, stats.Ventes.CommandesLivrees);

            Assert.Equal(4, stats.Devis.Crees);
            Assert.Equal(25m, stats.Devis.TauxAcceptationPourcent);
            Assert.Equal(5m, stats.Devis.RemiseMoyennePourcent);

            var tiktok = stats.AcquisitionParSource.Single(a => a.Source == "TikTok");
            Assert.Equal(2, tiktok.NouveauxClients);
            Assert.Equal(1, tiktok.ClientsAyantPaye);
            Assert.Equal(50m, tiktok.TauxConversionPourcent);
            Assert.Equal(600_000m, tiktok.Encaisse);
            Assert.DoesNotContain(stats.AcquisitionParSource, a => a.Source == "Facebook"); // hors période

            var top = Assert.Single(stats.TopProduitsLivres);
            Assert.Equal(3, top.Quantite); // 2 PC (TikTok) + 1 PC (WhatsApp)
            Assert.Equal(100m, stats.Livraisons.TauxReussitePourcent);
            Assert.Equal(24, stats.Livraisons.DelaiMoyenPaiementLivraisonHeures);
        }

        // ---------- Utilitaires ----------

        /// <summary>Client + commande payée (paiement confirmé à <paramref name="date"/>) et livrée un jour après.</summary>
        private (Commande Commande, Guid DevisId, Guid PaiementId) CreerCommandeLivree(
            DateTime date, SourceClient source = SourceClient.SiteWeb, decimal montant = 300_000m)
        {
            var client = CreerClient(source, date);
            var devisId = Guid.NewGuid();
            _context.Devis.Add(new DevisEntity { Id = devisId, Reference = $"DEV-{devisId:N}"[..20], ClientId = client.Id, DateCreation = date.AddDays(-30), Statut = StatutDevis.Accepte });

            var quantite = (int)(montant / 300_000m);
            var commande = new Commande
            {
                Id = Guid.NewGuid(), Reference = $"CMD-{Guid.NewGuid():N}"[..20], Statut = StatutCommande.Livree, ClientId = client.Id,
                DevisOrigineId = devisId, DateCreation = date,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+228" }
            };
            var version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = montant, Total = montant };
            version.Lignes.Add(new LigneCommande { Id = Guid.NewGuid(), ProduitId = _produitId, Quantite = quantite, PrixUnitaire = 300_000m, Total = montant });
            commande.Versions.Add(version);
            _context.Commandes.Add(commande);

            var paiementId = Guid.NewGuid();
            _context.Paiements.Add(new Paiement
            {
                Id = paiementId, Reference = $"PAY-{paiementId:N}"[..20], Montant = montant, Statut = StatutPaiement.Confirme, Mode = ModePaiement.Externe,
                DatePaiement = date, DateConfirmation = date, CommandeId = commande.Id, VersionCommandeId = version.Id
            });
            _context.Livraisons.Add(new Livraison
            {
                Id = Guid.NewGuid(), Reference = $"LIV-{Guid.NewGuid():N}"[..20], Type = TypeLivraison.Initial, Statut = StatutLivraison.Livree,
                DatePlanifiee = date, DatePriseEnCharge = date.AddHours(20), DateLivraison = date.AddDays(1), CommandeId = commande.Id
            });
            _context.SaveChanges();
            return (commande, devisId, paiementId);
        }

        private Client CreerClient(SourceClient source, DateTime date)
        {
            var client = new Client { Id = Guid.NewGuid(), CodeClient = $"CLI-{Guid.NewGuid():N}"[..16], Nom = "Client", Telephone = "+228", Source = source, DateCreation = date };
            _context.Clients.Add(client);
            _context.SaveChanges();
            return client;
        }

        private static JournalAudit Journal(string action, string entite, Guid entiteId, DateTime date, Guid? utilisateurId) => new()
        {
            Id = Guid.NewGuid(), Action = action, Entite = entite, EntiteId = entiteId, DateAction = date, UtilisateurId = utilisateurId
        };

        private void ConnecterAdmin()
        {
            _user.UtilisateurId = _utilisateurId;
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
