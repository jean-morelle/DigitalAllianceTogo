using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Devis.Commands.AccepterDevis;
using DigitalAllianceTogo.Application.Devis.Commands.CreerDevis;
using DigitalAllianceTogo.Application.Devis.Commands.EnvoyerDevis;
using DigitalAllianceTogo.Application.Devis.Commands.ModifierDevis;
using DigitalAllianceTogo.Application.Devis.Commands.RepondreDevis;
using DigitalAllianceTogo.Application.Devis.Commands.ValiderDevis;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Application.Devis.Queries.GetDevisById;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Infrastructure.Persistence;
using DigitalAllianceTogo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Tests.Devis
{
    /// <summary>
    /// Règles métier du devis (cahier des charges §7-8) testées sur une base en mémoire.
    /// </summary>
    public class DevisWorkflowTests
    {
        private const decimal PrixProduit = 100_000m;

        private readonly ApplicationDbContext _context;
        private readonly FakeCurrentUser _user = new();
        private readonly AuditService _audit;

        private readonly Guid _produitId = Guid.NewGuid();
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly Guid _clientUtilisateurId = Guid.NewGuid();
        private readonly Guid _adresseId = Guid.NewGuid();
        private readonly Guid _commercialId = Guid.NewGuid();
        private readonly Guid _adminId = Guid.NewGuid();

        public DevisWorkflowTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated(); // crée aussi les ParametresEntreprise par défaut (seuil 10 %)
            _audit = new AuditService(_context, _user);

            var categorie = new Categorie { Id = Guid.NewGuid(), Nom = "Ordinateurs portables" };
            var marque = new Marque { Id = Guid.NewGuid(), Nom = "HP" };
            _context.Categories.Add(categorie);
            _context.Marques.Add(marque);
            _context.Produits.Add(new Produit
            {
                Id = _produitId, Reference = "HP-250", Nom = "HP 250 G9", Prix = PrixProduit,
                CategorieId = categorie.Id, MarqueId = marque.Id
            });
            _context.Utilisateurs.Add(new Utilisateur
            {
                Id = _clientUtilisateurId, Nom = "Koffi", Prenom = "Ama", Email = "ama@test.tg", Telephone = "+22890000000"
            });
            _context.Clients.Add(new Client { Id = _clientId, CodeClient = "CLI-001", UtilisateurId = _clientUtilisateurId });
            _context.Adresses.Add(new Adresse
            {
                Id = _adresseId, ClientId = _clientId, Libelle = "Maison", Ligne1 = "Rue 123", Ville = "Lomé", Pays = "Togo"
            });
            _context.SaveChanges();
        }

        // ---------- Création ----------

        [Fact]
        public async Task Creer_calcule_les_totaux_avec_le_prix_catalogue()
        {
            var id = await CreerDevisAsync(quantite: 2, remiseLigne: 5_000, remiseGlobale: 5_000);

            var devis = await _context.Devis.Include(d => d.Lignes).SingleAsync(d => d.Id == id);
            Assert.Equal(StatutDevis.Brouillon, devis.Statut);
            Assert.Equal(200_000m, devis.SousTotal);
            Assert.Equal(10_000m, devis.Remise);
            Assert.Equal(190_000m, devis.Total);
            Assert.Equal(5m, devis.TauxRemise);
            Assert.Equal(PrixProduit, devis.Lignes.Single().PrixUnitaire);
            Assert.Equal(_commercialId, devis.CreeParId);
            Assert.True(await _context.JournauxAudit.AnyAsync(j => j.EntiteId == id && j.Action == "CreationDevis"));
        }

        [Fact]
        public async Task Creer_refuse_une_remise_superieure_au_montant()
        {
            await Assert.ThrowsAsync<ConflictException>(() => CreerDevisAsync(quantite: 1, remiseLigne: 0, remiseGlobale: 150_000));
        }

        // ---------- Règle des 10 % ----------

        [Fact]
        public async Task Commercial_valide_lui_meme_une_remise_egale_au_seuil()
        {
            var id = await CreerDevisAsync(quantite: 1, remiseGlobale: 10_000); // 10 %

            var result = await ValiderAsync(id, Roles.Commercial);

            Assert.True(result.Valide);
            var devis = await _context.Devis.SingleAsync(d => d.Id == id);
            Assert.Equal(_commercialId, devis.ValideParId);
        }

        [Fact]
        public async Task Remise_au_dessus_du_seuil_est_soumise_a_l_administrateur()
        {
            var id = await CreerDevisAsync(quantite: 1, remiseGlobale: 15_000); // 15 %

            var result = await ValiderAsync(id, Roles.Commercial);

            Assert.False(result.Valide);
            var devis = await _context.Devis.SingleAsync(d => d.Id == id);
            Assert.Equal(StatutDevis.ValidationInterne, devis.Statut);
            Assert.Null(devis.ValideParId);

            // Le commercial ne peut pas l'envoyer tant que l'Admin n'a pas validé
            await Assert.ThrowsAsync<ConflictException>(() => EnvoyerAsync(id));
        }

        [Fact]
        public async Task Administrateur_valide_une_remise_exceptionnelle()
        {
            var id = await CreerDevisAsync(quantite: 1, remiseGlobale: 15_000);
            await ValiderAsync(id, Roles.Commercial);

            var result = await ValiderAsync(id, Roles.Admin);

            Assert.True(result.Valide);
            Assert.Equal(_adminId, (await _context.Devis.SingleAsync(d => d.Id == id)).ValideParId);
            await EnvoyerAsync(id); // ne lève pas
        }

        [Fact]
        public async Task Modifier_un_devis_valide_annule_la_validation()
        {
            var id = await CreerDevisAsync(quantite: 1);
            await ValiderAsync(id, Roles.Commercial);

            ConnecterCommePersonnel(Roles.Commercial);
            await new ModifierDevisCommandHandler(_context, _user, _audit).Handle(new ModifierDevisCommand
            {
                Id = id,
                Lignes = { new LigneDevisInput { ProduitId = _produitId, Quantite = 3 } }
            }, default);

            var devis = await _context.Devis.SingleAsync(d => d.Id == id);
            Assert.Null(devis.ValideParId);
            Assert.Equal(300_000m, devis.Total);
        }

        // ---------- Réponse du client ----------

        [Fact]
        public async Task Acceptation_cree_automatiquement_la_commande_version_1()
        {
            var id = await CreerEtEnvoyerAsync(quantite: 2, remiseGlobale: 10_000);

            var commandeId = await AccepterAsync(id);

            var commande = await _context.Commandes
                .Include(c => c.Versions).ThenInclude(v => v.Lignes)
                .Include(c => c.AdresseLivraison)
                .SingleAsync(c => c.Id == commandeId);
            var devis = await _context.Devis.SingleAsync(d => d.Id == id);

            Assert.Equal(StatutDevis.Accepte, devis.Statut); // le devis reste conservé
            Assert.Equal(StatutCommande.CommandeCreee, commande.Statut);
            Assert.Equal(id, commande.DevisOrigineId);
            var version = Assert.Single(commande.Versions);
            Assert.Equal(1, version.NumeroVersion);
            Assert.True(version.Active);
            Assert.Equal(devis.Total, version.Total);
            Assert.Equal(2, version.Lignes.Single().Quantite);
            Assert.Equal("Lomé", commande.AdresseLivraison.Ville);
            Assert.Equal("+22890000000", commande.AdresseLivraison.TelephoneContact);
        }

        [Fact]
        public async Task Un_devis_ne_peut_pas_etre_accepte_deux_fois()
        {
            var id = await CreerEtEnvoyerAsync(quantite: 1);
            await AccepterAsync(id);

            await Assert.ThrowsAsync<ConflictException>(() => AccepterAsync(id));
            Assert.Equal(1, await _context.Commandes.CountAsync());
        }

        [Fact]
        public async Task Un_devis_expire_ne_peut_pas_etre_accepte()
        {
            var id = await CreerEtEnvoyerAsync(quantite: 1);
            var devis = await _context.Devis.SingleAsync(d => d.Id == id);
            devis.DateValidite = DateTime.UtcNow.AddDays(-1);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ConflictException>(() => AccepterAsync(id));

            Assert.Equal(StatutDevis.Expire, (await _context.Devis.SingleAsync(d => d.Id == id)).Statut);
            Assert.False(await _context.Commandes.AnyAsync());
        }

        [Fact]
        public async Task Demande_de_modification_exige_un_commentaire_et_change_le_statut()
        {
            var id = await CreerEtEnvoyerAsync(quantite: 1);
            ConnecterCommeClient(_clientUtilisateurId);

            await new RepondreDevisCommandHandler(_context, _user, _audit).Handle(new RepondreDevisCommand
            {
                Id = id,
                Reponse = ReponseClientDevis.DemanderModification,
                Commentaire = "Je voudrais 16 Go de RAM"
            }, default);

            var devis = await _context.Devis.SingleAsync(d => d.Id == id);
            Assert.Equal(StatutDevis.ModificationDemandee, devis.Statut);
            Assert.Equal("Je voudrais 16 Go de RAM", devis.CommentaireClient);

            var validation = new RepondreDevisCommandValidator()
                .Validate(new RepondreDevisCommand { Id = id, Reponse = ReponseClientDevis.DemanderModification });
            Assert.False(validation.IsValid);
        }

        // ---------- Contrôle d'accès ----------

        [Fact]
        public async Task Un_client_ne_peut_pas_voir_le_devis_d_un_autre_client()
        {
            var id = await CreerDevisAsync(quantite: 1);
            ConnecterCommeClient(Guid.NewGuid()); // autre client

            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                new GetDevisByIdQueryHandler(_context, _user).Handle(new GetDevisByIdQuery(id), default));
        }

        [Fact]
        public async Task Le_client_ne_voit_pas_les_commentaires_internes()
        {
            var id = await CreerDevisAsync(quantite: 1);
            var devis = await _context.Devis.SingleAsync(d => d.Id == id);
            devis.CommentaireInterne = "Remise refusée par l'admin";
            await _context.SaveChangesAsync();
            ConnecterCommeClient(_clientUtilisateurId);

            var dto = await new GetDevisByIdQueryHandler(_context, _user).Handle(new GetDevisByIdQuery(id), default);

            Assert.Null(dto.CommentaireInterne);
        }

        // ---------- Helpers ----------

        private async Task<Guid> CreerDevisAsync(int quantite, decimal remiseLigne = 0, decimal remiseGlobale = 0)
        {
            ConnecterCommePersonnel(Roles.Commercial);
            return await new CreerDevisCommandHandler(_context, _user, _audit).Handle(new CreerDevisCommand
            {
                ClientId = _clientId,
                RemiseGlobale = remiseGlobale,
                Lignes = { new LigneDevisInput { ProduitId = _produitId, Quantite = quantite, Remise = remiseLigne } }
            }, default);
        }

        private Task<ValiderDevisResult> ValiderAsync(Guid id, string role)
        {
            ConnecterCommePersonnel(role);
            return new ValiderDevisCommandHandler(_context, _user, _audit).Handle(new ValiderDevisCommand(id), default);
        }

        private Task EnvoyerAsync(Guid id)
        {
            ConnecterCommePersonnel(Roles.Commercial);
            return new EnvoyerDevisCommandHandler(_context, _user, _audit).Handle(new EnvoyerDevisCommand(id), default);
        }

        private async Task<Guid> CreerEtEnvoyerAsync(int quantite, decimal remiseGlobale = 0)
        {
            var id = await CreerDevisAsync(quantite, remiseGlobale: remiseGlobale);
            await ValiderAsync(id, Roles.Commercial);
            await EnvoyerAsync(id);
            return id;
        }

        private Task<Guid> AccepterAsync(Guid id)
        {
            ConnecterCommeClient(_clientUtilisateurId);
            return new AccepterDevisCommandHandler(_context, _user, _audit)
                .Handle(new AccepterDevisCommand { Id = id, AdresseLivraisonId = _adresseId }, default);
        }

        private void ConnecterCommePersonnel(string role)
        {
            _user.UtilisateurId = role == Roles.Admin ? _adminId : _commercialId;
            _user.Roles = new[] { role };
        }

        private void ConnecterCommeClient(Guid utilisateurId)
        {
            _user.UtilisateurId = utilisateurId;
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
