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

namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    /// <summary>
    /// Abstraction du DbContext vue par l'Application. L'implémentation concrète
    /// (EF Core + PostgreSQL) vit dans GkasGroup.Infrastructure et n'est jamais
    /// référencée directement ici : l'Application ne connaît que cette interface.
    /// </summary>
    public interface IApplicationDbContext
    {
        // Security
        DbSet<Utilisateur> Utilisateurs { get; }
        DbSet<Client> Clients { get; }
        DbSet<Adresse> Adresses { get; }
        DbSet<Role> Roles { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<UtilisateurRole> UtilisateurRoles { get; }
        DbSet<RolePermission> RolePermissions { get; }

        // Catalogue
        DbSet<Produit> Produits { get; }
        DbSet<Categorie> Categories { get; }
        DbSet<Marque> Marques { get; }
        DbSet<ImageProduit> ImagesProduit { get; }
        DbSet<AttributProduit> AttributsProduit { get; }

        // Stock
        DbSet<Entrepot> Entrepots { get; }
        DbSet<StockProduit> StocksProduit { get; }
        DbSet<MouvementStock> MouvementsStock { get; }
        DbSet<EcartReception> EcartsReception { get; }

        // Devis
        // Nom complet : "Devis" est aussi le namespace du module Application.Devis
        DbSet<DigitalAllianceTogo.Domain.Models.Devis.Devis> Devis { get; }
        DbSet<LigneDevis> LignesDevis { get; }

        // Panier
        DbSet<Panier> Paniers { get; }
        DbSet<LignePanier> LignesPanier { get; }

        // Commande
        DbSet<Commande> Commandes { get; }
        DbSet<VersionCommande> VersionsCommande { get; }
        DbSet<LigneCommande> LignesCommande { get; }
        DbSet<AdresseLivraisonCommande> AdressesLivraisonCommande { get; }

        // Finance
        DbSet<Paiement> Paiements { get; }
        DbSet<Remboursement> Remboursements { get; }
        DbSet<Avoir> Avoirs { get; }

        // Livraison
        DbSet<Livraison> Livraisons { get; }
        DbSet<PreuveLivraison> PreuvesLivraison { get; }

        // SAV
        DbSet<TicketSAV> TicketsSAV { get; }
        DbSet<Diagnostic> Diagnostics { get; }
        DbSet<Intervention> Interventions { get; }

        // Audit
        DbSet<JournalAudit> JournauxAudit { get; }

        // Paramètres
        DbSet<ParametresEntreprise> ParametresEntreprise { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
