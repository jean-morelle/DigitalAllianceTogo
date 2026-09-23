using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Notifications;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Finance;
using DigitalAllianceTogo.Domain.Models.Livraison;
using DigitalAllianceTogo.Domain.Models.Notifications;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Notifications
{
    /// <summary>Notifications client : déduites des changements de statut, puis envoyées par e-mail.</summary>
    public class NotificationsTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _utilisateurClientId = Guid.NewGuid();
        private readonly Commande _commande;
        private readonly VersionCommande _version;

        public NotificationsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _context.Utilisateurs.Add(new Utilisateur { Id = _utilisateurClientId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg" });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _utilisateurClientId, Nom = "Koffi", Telephone = "+22890000000" });
            _commande = new Commande
            {
                Id = Guid.NewGuid(),
                Reference = "CMD-001",
                Statut = StatutCommande.PaiementEnAttente,
                ClientId = _clientId,
                AdresseLivraison = new AdresseLivraisonCommande { Id = Guid.NewGuid(), Ligne1 = "Rue 1", Ville = "Lomé", Pays = "Togo", TelephoneContact = "+22890000000" }
            };
            _version = new VersionCommande { Id = Guid.NewGuid(), NumeroVersion = 1, SousTotal = 700_000m, Total = 700_000m };
            _commande.Versions.Add(_version);
            _context.Commandes.Add(_commande);
            _context.SaveChanges(); // synchrone : pas de détection (création de la commande par le client lui-même)
            _user.UtilisateurId = _utilisateurClientId;
        }

        private Task<List<Notification>> NotificationsAsync() =>
            _context.Notifications.AsNoTracking().OrderBy(n => n.DateCreation).ToListAsync();

        [Fact]
        public async Task Paiement_confirme_avec_reservation_une_seule_notification()
        {
            // ConfirmerPaiement passe par PaiementConfirme puis StockReserve dans la même sauvegarde
            _commande.Statut = StatutCommande.PaiementConfirme;
            _commande.Statut = StatutCommande.StockReserve;
            await _context.SaveChangesAsync();

            var n = Assert.Single(await NotificationsAsync());
            Assert.Equal("PaiementConfirme", n.Type);
            Assert.Equal(_clientId, n.ClientId);
            Assert.Equal($"/compte/commandes/{_commande.Id}", n.Lien);
            Assert.Contains("CMD-001", n.Message);
        }

        [Fact]
        public async Task Paiement_refuse_donne_le_motif()
        {
            _context.Paiements.Add(new Paiement
            {
                Id = Guid.NewGuid(), Reference = "PAY-1", Montant = 700_000m, CommandeId = _commande.Id, VersionCommandeId = _version.Id,
                Statut = StatutPaiement.Echoue, MotifRejet = "référence introuvable"
            });
            _commande.Statut = StatutCommande.PaiementEchoue;
            await _context.SaveChangesAsync();

            var n = Assert.Single(await NotificationsAsync());
            Assert.Equal("PaiementRefuse", n.Type);
            Assert.Contains("référence introuvable", n.Message);
        }

        [Fact]
        public async Task Livraison_planifiee_puis_echouee()
        {
            var livraison = new Livraison
            {
                Id = Guid.NewGuid(), Reference = "LIV-1", CommandeId = _commande.Id,
                DatePlanifiee = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc)
            };
            _context.Livraisons.Add(livraison);
            await _context.SaveChangesAsync();

            livraison.Statut = StatutLivraison.Echouee;
            livraison.MotifEchec = "client absent";
            await _context.SaveChangesAsync();

            var notifications = await NotificationsAsync();
            Assert.Equal(new[] { "LivraisonPlanifiee", "LivraisonEchouee" }, notifications.Select(n => n.Type));
            Assert.Contains("vendredi 25 septembre", notifications[0].Message);
            Assert.Contains("client absent", notifications[1].Message);
        }

        [Fact]
        public async Task Un_changement_sans_interet_pour_le_client_ne_notifie_pas()
        {
            _commande.Statut = StatutCommande.PaiementConfirme;
            await _context.SaveChangesAsync();
            _commande.Statut = StatutCommande.PreparationEnCours;
            await _context.SaveChangesAsync();

            Assert.Single(await NotificationsAsync()); // seulement le paiement
        }

        [Fact]
        public async Task Le_client_ne_voit_et_ne_lit_que_ses_notifications()
        {
            var autreClient = new Client { Id = Guid.NewGuid(), CodeClient = "CLI-002", Nom = "Autre", Telephone = "+22891000000" };
            _context.Clients.Add(autreClient);
            var autre = new Notification { Id = Guid.NewGuid(), ClientId = autreClient.Id, Type = "X", Titre = "X", Message = "X" };
            _context.Notifications.AddRange(autre,
                new Notification { Id = Guid.NewGuid(), ClientId = _clientId, Type = "Y", Titre = "Y", Message = "Y" });
            await _context.SaveChangesAsync();

            var handler = new MesNotificationsHandler(_context, _user);
            Assert.Equal(1, await handler.Handle(new GetNombreNonLuesQuery(), default));
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new MarquerLuesCommand(autre.Id), default));

            await handler.Handle(new MarquerLuesCommand(null), default);
            Assert.Equal(0, await handler.Handle(new GetNombreNonLuesQuery(), default));
            Assert.Null((await _context.Notifications.AsNoTracking().SingleAsync(n => n.Id == autre.Id)).DateLecture);
        }

        [Fact]
        public async Task Envoi_email_adresse_du_compte_puis_abandon_apres_echecs()
        {
            _commande.Statut = StatutCommande.StockReserve;
            await _context.SaveChangesAsync();
            var email = new FauxEmail();

            await new EnvoyerNotificationsEmailCommandHandler(_context, email).Handle(new EnvoyerNotificationsEmailCommand(), default);
            var envoi = Assert.Single(email.Envois);
            Assert.Equal("ama@test.tg", envoi.Destinataire);
            Assert.Contains($"https://togo.test/compte/commandes/{_commande.Id}", envoi.Corps);
            Assert.Equal(StatutEnvoiNotification.Envoye, (await NotificationsAsync()).Single().StatutEmail);

            // Serveur en panne : nouvel essai à chaque passage, abandon au 5e
            _commande.Statut = StatutCommande.EnTransit;
            await _context.SaveChangesAsync();
            email.EnPanne = true;
            for (var i = 0; i < EnvoyerNotificationsEmailCommandHandler.TentativesMax; i++)
                await new EnvoyerNotificationsEmailCommandHandler(_context, email).Handle(new EnvoyerNotificationsEmailCommand(), default);
            var enRoute = (await NotificationsAsync()).Single(n => n.Type == "CommandeEnRoute");
            Assert.Equal(StatutEnvoiNotification.Echec, enRoute.StatutEmail);
            Assert.Equal("SMTP indisponible", enRoute.ErreurEmail);
        }

        [Fact]
        public async Task Sans_serveur_configure_la_notification_reste_dans_l_espace_client()
        {
            _commande.Statut = StatutCommande.StockReserve;
            await _context.SaveChangesAsync();

            await new EnvoyerNotificationsEmailCommandHandler(_context, new FauxEmail { EstConfigure = false }).Handle(new EnvoyerNotificationsEmailCommand(), default);

            Assert.Equal(StatutEnvoiNotification.NonEnvoye, (await NotificationsAsync()).Single().StatutEmail);
        }

        private sealed class FauxEmail : IEnvoiEmail
        {
            public List<(string Destinataire, string Sujet, string Corps)> Envois { get; } = new();
            public bool EnPanne { get; set; }
            public bool EstConfigure { get; init; } = true;
            public string LienSite(string chemin) => "https://togo.test" + chemin;

            public Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml, CancellationToken cancellationToken)
            {
                if (EnPanne) throw new InvalidOperationException("SMTP indisponible");
                Envois.Add((destinataire, sujet, corpsHtml));
                return Task.CompletedTask;
            }
        }

        private sealed class FakeCurrentUser : ICurrentUserService
        {
            public Guid? UtilisateurId { get; set; }
            public bool EstAuthentifie => UtilisateurId.HasValue;
            public string? AdresseIP => "127.0.0.1";
            public bool EstDansRole(string role) => false;
        }
    }
}
