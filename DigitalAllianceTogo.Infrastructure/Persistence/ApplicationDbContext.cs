using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Audit;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using DigitalAllianceTogo.Domain.Models.Commande;
using DigitalAllianceTogo.Domain.Models.Devis;
using DigitalAllianceTogo.Domain.Models.Finance;
using DigitalAllianceTogo.Domain.Models.Livraison;
using DigitalAllianceTogo.Domain.Models.Panier;
using DigitalAllianceTogo.Domain.Models.Parametres;
using DigitalAllianceTogo.Domain.Models.SAV;
using DigitalAllianceTogo.Domain.Models.Security;
using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;


namespace DigitalAllianceTogo.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }
        // ----- Security -----
        public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Adresse> Adresses => Set<Adresse>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UtilisateurRole> UtilisateurRoles => Set<UtilisateurRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        // ----- Catalogue -----
        public DbSet<Produit> Produits => Set<Produit>();
        public DbSet<Categorie> Categories => Set<Categorie>();
        public DbSet<Marque> Marques => Set<Marque>();
        public DbSet<ImageProduit> ImagesProduit => Set<ImageProduit>();
        public DbSet<AttributProduit> AttributsProduit => Set<AttributProduit>();

        // ----- Stock -----
        public DbSet<Entrepot> Entrepots => Set<Entrepot>();
        public DbSet<StockProduit> StocksProduit => Set<StockProduit>();
        public DbSet<MouvementStock> MouvementsStock => Set<MouvementStock>();

        // ----- Devis -----
        public DbSet<Devis> Devis => Set<Devis>();
        public DbSet<LigneDevis> LignesDevis => Set<LigneDevis>();

        // ----- Panier -----
        public DbSet<Panier> Paniers => Set<Panier>();
        public DbSet<LignePanier> LignesPanier => Set<LignePanier>();

        // ----- Commande -----
        public DbSet<Commande> Commandes => Set<Commande>();
        public DbSet<VersionCommande> VersionsCommande => Set<VersionCommande>();
        public DbSet<LigneCommande> LignesCommande => Set<LigneCommande>();
        public DbSet<AdresseLivraisonCommande> AdressesLivraisonCommande => Set<AdresseLivraisonCommande>();

        // ----- Finance -----
        public DbSet<Paiement> Paiements => Set<Paiement>();
        public DbSet<Remboursement> Remboursements => Set<Remboursement>();
        public DbSet<Avoir> Avoirs => Set<Avoir>();

        // ----- Livraison -----
        public DbSet<Livraison> Livraisons => Set<Livraison>();
        public DbSet<PreuveLivraison> PreuvesLivraison => Set<PreuveLivraison>();

        // ----- SAV -----
        public DbSet<TicketSAV> TicketsSAV => Set<TicketSAV>();
        public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();
        public DbSet<Intervention> Interventions => Set<Intervention>();

        // ----- Audit -----
        public DbSet<JournalAudit> JournauxAudit => Set<JournalAudit>();

        // ----- Paramètres -----
        public DbSet<ParametresEntreprise> ParametresEntreprise => Set<ParametresEntreprise>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // Convention globale : tous les decimal du modèle sont stockés en decimal(18,2),
            // évite de répéter .HasPrecision(18,2) dans chacune des 31 configurations.
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Charge automatiquement toutes les classes IEntityTypeConfiguration<T>
            // présentes dans cet assembly (dossier Persistence/Configurations/**).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
