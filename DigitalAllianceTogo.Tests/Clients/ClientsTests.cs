using DigitalAllianceTogo.Application.Clients.Commands.Adresses;
using DigitalAllianceTogo.Application.Clients.Commands.CreerClient;
using DigitalAllianceTogo.Application.Clients.Commands.InscrireClient;
using DigitalAllianceTogo.Application.Clients.Commands.ModifierClient;
using DigitalAllianceTogo.Application.Clients.Queries.GetClientById;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Clients
{
    public class ClientsTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;
        private readonly PasswordHasher _hasher = new();

        public ClientsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _context.Roles.Add(new Role { Id = Guid.NewGuid(), Nom = Roles.Client });
            _context.SaveChanges();
            _audit = new AuditService(_context, _user);
        }

        [Fact]
        public async Task Commercial_cree_un_prospect_WhatsApp_sans_compte()
        {
            var id = await CreerProspectAsync("+228 90 11 22 33");

            var client = await _context.Clients.SingleAsync(c => c.Id == id);
            Assert.Equal(SourceClient.WhatsApp, client.Source);
            Assert.Null(client.UtilisateurId);
            Assert.StartsWith("CLI-", client.CodeClient);
            Assert.True(await _context.JournauxAudit.AnyAsync(j => j.EntiteId == id && j.Action == "CreationClient"));
        }

        [Fact]
        public async Task Commercial_ne_peut_pas_creer_deux_fois_le_meme_telephone()
        {
            await CreerProspectAsync("+22890112233");
            await Assert.ThrowsAsync<ConflictException>(() => CreerProspectAsync("+22890112233"));
        }

        [Fact]
        public void Une_entreprise_doit_avoir_une_raison_sociale()
        {
            var result = new CreerClientCommandValidator().Validate(new CreerClientCommand
            {
                Type = TypeClient.Entreprise, Nom = "Mensah", Telephone = "+22890112233"
            });

            Assert.Contains(result.Errors, e => e.PropertyName == "RaisonSociale");
        }

        [Fact]
        public async Task Inscription_cree_compte_role_client_et_fiche()
        {
            var id = await InscrireAsync("Ama@Test.TG", "+22891000000");

            var client = await _context.Clients.Include(c => c.Utilisateur).ThenInclude(u => u!.UtilisateurRoles).ThenInclude(ur => ur.Role)
                .SingleAsync(c => c.Id == id);
            Assert.NotNull(client.Utilisateur);
            Assert.Equal("ama@test.tg", client.Utilisateur!.Email); // email normalisé
            Assert.Equal(Roles.Client, client.Utilisateur.UtilisateurRoles.Single().Role.Nom);
            Assert.True(_hasher.Verify("MotDePasse1", client.Utilisateur.MotDePasseHash));
            Assert.Equal(SourceClient.TikTok, client.Source);
        }

        [Fact]
        public async Task Inscription_refuse_un_email_deja_utilise_quelle_que_soit_la_casse()
        {
            await InscrireAsync("ama@test.tg", "+22891000000");
            await Assert.ThrowsAsync<ConflictException>(() => InscrireAsync("AMA@test.tg", "+22891000001"));
        }

        [Fact]
        public async Task Inscription_ne_rattache_jamais_une_fiche_existante_par_le_seul_telephone()
        {
            // Sécurité : sinon n'importe qui connaissant le numéro récupérerait l'historique du prospect
            var prospectId = await CreerProspectAsync("+22890112233");

            var inscritId = await InscrireAsync("autre@test.tg", "+22890112233");

            Assert.NotEqual(prospectId, inscritId);
            Assert.Null((await _context.Clients.SingleAsync(c => c.Id == prospectId)).UtilisateurId);
        }

        [Fact]
        public async Task Un_client_ne_peut_pas_voir_la_fiche_d_un_autre()
        {
            var prospectId = await CreerProspectAsync("+22890112233");
            var moiId = await InscrireAsync("ama@test.tg", "+22891000000");
            await ConnecterClientAsync(moiId);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new GetClientByIdQueryHandler(_context, _user).Handle(new GetClientByIdQuery(prospectId), default));

            var maFiche = await new GetClientByIdQueryHandler(_context, _user).Handle(new GetClientByIdQuery(null), default);
            Assert.Equal(moiId, maFiche.Id);
        }

        [Fact]
        public async Task Client_modifie_sa_fiche_sans_pouvoir_changer_sa_source_et_le_compte_suit()
        {
            var id = await InscrireAsync("ama@test.tg", "+22891000000");
            await ConnecterClientAsync(id);

            await new ModifierClientCommandHandler(_context, _user, _audit).Handle(new ModifierClientCommand
            {
                Id = id, Nom = "Koffi", Prenom = "Ama", Telephone = "+22899999999",
                Email = "pirate@test.tg", Source = SourceClient.Boutique
            }, default);

            var client = await _context.Clients.Include(c => c.Utilisateur).SingleAsync(c => c.Id == id);
            Assert.Equal("+22899999999", client.Telephone);
            Assert.Equal("+22899999999", client.Utilisateur!.Telephone); // répercuté sur le compte
            Assert.Equal("ama@test.tg", client.Email);                   // email de connexion inchangé
            Assert.Equal(SourceClient.TikTok, client.Source);            // source réservée au personnel
        }

        [Fact]
        public async Task Un_client_ne_peut_pas_supprimer_l_adresse_d_un_autre()
        {
            var prospectId = await CreerProspectAsync("+22890112233");
            ConnecterPersonnel();
            var adresseId = await new AdresseCommandsHandler(_context, _user).Handle(new AjouterAdresseCommand
            {
                ClientId = prospectId, Libelle = "Bureau", Ligne1 = "Bd du 13 Janvier", Ville = "Lomé"
            }, default);

            var moiId = await InscrireAsync("ama@test.tg", "+22891000000");
            await ConnecterClientAsync(moiId);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new AdresseCommandsHandler(_context, _user).Handle(new SupprimerAdresseCommand(prospectId, adresseId), default));
            Assert.True(await _context.Adresses.AnyAsync(a => a.Id == adresseId));
        }

        // ---------- Helpers ----------

        private Task<Guid> CreerProspectAsync(string telephone)
        {
            ConnecterPersonnel();
            return new CreerClientCommandHandler(_context, _audit).Handle(new CreerClientCommand
            {
                Nom = "Mensah", Prenom = "Kodjo", Telephone = telephone, Source = SourceClient.WhatsApp
            }, default);
        }

        private Task<Guid> InscrireAsync(string email, string telephone)
        {
            _user.UtilisateurId = null; // inscription anonyme
            _user.Roles = Array.Empty<string>();
            return new InscrireClientCommandHandler(_context, _hasher, _audit).Handle(new InscrireClientCommand
            {
                Nom = "Koffi", Prenom = "Ama", Email = email, Telephone = telephone,
                MotDePasse = "MotDePasse1", Source = SourceClient.TikTok
            }, default);
        }

        private void ConnecterPersonnel()
        {
            _user.UtilisateurId = Guid.NewGuid();
            _user.Roles = new[] { Roles.Commercial };
        }

        private async Task ConnecterClientAsync(Guid clientId)
        {
            _user.UtilisateurId = (await _context.Clients.SingleAsync(c => c.Id == clientId)).UtilisateurId;
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
