using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entrepots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Adresse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrepots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Telephone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "text", nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Prix = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CategorieId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarqueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produits_Categories_CategorieId",
                        column: x => x.CategorieId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Produits_Marques_MarqueId",
                        column: x => x.MarqueId,
                        principalTable: "Marques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeClient = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JournauxAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateAction = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Entite = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AncienneValeur = table.Column<string>(type: "text", nullable: true),
                    NouvelleValeur = table.Column<string>(type: "text", nullable: true),
                    AdresseIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournauxAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournauxAudit_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Paniers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paniers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paniers_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilisateurRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateAffectation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilisateurRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilisateurRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UtilisateurRoles_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttributsProduit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Cle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Valeur = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributsProduit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributsProduit_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagesProduit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    EstPrincipale = table.Column<bool>(type: "boolean", nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagesProduit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagesProduit_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StocksProduit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantitePhysique = table.Column<int>(type: "integer", nullable: false),
                    QuantiteReservee = table.Column<int>(type: "integer", nullable: false),
                    SeuilAlerte = table.Column<int>(type: "integer", nullable: false),
                    EntrepotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocksProduit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocksProduit_Entrepots_EntrepotId",
                        column: x => x.EntrepotId,
                        principalTable: "Entrepots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocksProduit_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Adresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ligne1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ligne2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ville = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Pays = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodePostal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adresses_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateValidite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Statut = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SousTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remise = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devis_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LignesPanier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DateAjout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PanierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesPanier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesPanier_Paniers_PanierId",
                        column: x => x.PanierId,
                        principalTable: "Paniers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LignesPanier_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MouvementsStock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    DateMouvement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motif = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StockProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MouvementsStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MouvementsStock_StocksProduit_StockProduitId",
                        column: x => x.StockProduitId,
                        principalTable: "StocksProduit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Commandes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VersionActive = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DevisOrigineId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commandes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Commandes_Devis_DevisOrigineId",
                        column: x => x.DevisOrigineId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LignesDevis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remise = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DevisId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesDevis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesDevis_Devis_DevisId",
                        column: x => x.DevisId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LignesDevis_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdressesLivraisonCommande",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ligne1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ligne2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ville = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Pays = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodePostal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TelephoneContact = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdressesLivraisonCommande", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdressesLivraisonCommande_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Livraisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Statut = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DatePlanifiee = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DatePriseEnCharge = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateLivraison = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LivreurId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Livraisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Livraisons_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Livraisons_Utilisateurs_LivreurId",
                        column: x => x.LivreurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VersionsCommande",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroVersion = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MotifModification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SousTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remise = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionsCommande", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersionsCommande_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreuvesLivraison",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DatePreuve = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SignatureUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Commentaire = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LivraisonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreuvesLivraison", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreuvesLivraison_Livraisons_LivraisonId",
                        column: x => x.LivraisonId,
                        principalTable: "Livraisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Avoirs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUtilisation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Statut = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionCommandeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avoirs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avoirs_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Avoirs_VersionsCommande_VersionCommandeId",
                        column: x => x.VersionCommandeId,
                        principalTable: "VersionsCommande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LignesCommande",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remise = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VersionCommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesCommande", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesCommande_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LignesCommande_VersionsCommande_VersionCommandeId",
                        column: x => x.VersionCommandeId,
                        principalTable: "VersionsCommande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DatePaiement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Statut = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionCommandeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paiements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paiements_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_VersionsCommande_VersionCommandeId",
                        column: x => x.VersionCommandeId,
                        principalTable: "VersionsCommande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Remboursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DateDemande = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Statut = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CommandeId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionCommandeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remboursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remboursements_Commandes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "Commandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Remboursements_VersionsCommande_VersionCommandeId",
                        column: x => x.VersionCommandeId,
                        principalTable: "VersionsCommande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketsSAV",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motif = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    LigneCommandeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketsSAV", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketsSAV_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketsSAV_LignesCommande_LigneCommandeId",
                        column: x => x.LigneCommandeId,
                        principalTable: "LignesCommande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Diagnostics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateDiagnostic = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Conclusion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Reparable = table.Column<bool>(type: "boolean", nullable: false),
                    Recommandation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TicketSAVId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicienId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnostics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diagnostics_TicketsSAV_TicketSAVId",
                        column: x => x.TicketSAVId,
                        principalTable: "TicketsSAV",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Diagnostics_Utilisateurs_TechnicienId",
                        column: x => x.TechnicienId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Interventions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Resultat = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TicketSAVId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicienId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interventions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interventions_TicketsSAV_TicketSAVId",
                        column: x => x.TicketSAVId,
                        principalTable: "TicketsSAV",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interventions_Utilisateurs_TechnicienId",
                        column: x => x.TechnicienId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adresses_ClientId",
                table: "Adresses",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AdressesLivraisonCommande_CommandeId",
                table: "AdressesLivraisonCommande",
                column: "CommandeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributsProduit_ProduitId",
                table: "AttributsProduit",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Avoirs_CommandeId",
                table: "Avoirs",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Avoirs_Reference",
                table: "Avoirs",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avoirs_VersionCommandeId",
                table: "Avoirs",
                column: "VersionCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Nom",
                table: "Categories",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CodeClient",
                table: "Clients",
                column: "CodeClient",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_UtilisateurId",
                table: "Clients",
                column: "UtilisateurId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_ClientId",
                table: "Commandes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_DevisOrigineId",
                table: "Commandes",
                column: "DevisOrigineId");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_Reference",
                table: "Commandes",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devis_ClientId",
                table: "Devis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_Reference",
                table: "Devis",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diagnostics_TechnicienId",
                table: "Diagnostics",
                column: "TechnicienId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnostics_TicketSAVId",
                table: "Diagnostics",
                column: "TicketSAVId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagesProduit_ProduitId",
                table: "ImagesProduit",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_TechnicienId",
                table: "Interventions",
                column: "TechnicienId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_TicketSAVId",
                table: "Interventions",
                column: "TicketSAVId");

            migrationBuilder.CreateIndex(
                name: "IX_JournauxAudit_Entite_EntiteId",
                table: "JournauxAudit",
                columns: new[] { "Entite", "EntiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournauxAudit_UtilisateurId",
                table: "JournauxAudit",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesCommande_ProduitId",
                table: "LignesCommande",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesCommande_VersionCommandeId",
                table: "LignesCommande",
                column: "VersionCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesDevis_DevisId",
                table: "LignesDevis",
                column: "DevisId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesDevis_ProduitId",
                table: "LignesDevis",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesPanier_PanierId_ProduitId",
                table: "LignesPanier",
                columns: new[] { "PanierId", "ProduitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LignesPanier_ProduitId",
                table: "LignesPanier",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Livraisons_CommandeId",
                table: "Livraisons",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Livraisons_LivreurId",
                table: "Livraisons",
                column: "LivreurId");

            migrationBuilder.CreateIndex(
                name: "IX_Livraisons_Reference",
                table: "Livraisons",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marques_Nom",
                table: "Marques",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_StockProduitId",
                table: "MouvementsStock",
                column: "StockProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_CommandeId",
                table: "Paiements",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_Reference",
                table: "Paiements",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_VersionCommandeId",
                table: "Paiements",
                column: "VersionCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Paniers_UtilisateurId",
                table: "Paniers",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreuvesLivraison_LivraisonId",
                table: "PreuvesLivraison",
                column: "LivraisonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produits_CategorieId",
                table: "Produits",
                column: "CategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Produits_MarqueId",
                table: "Produits",
                column: "MarqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Produits_Reference",
                table: "Produits",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Remboursements_CommandeId",
                table: "Remboursements",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Remboursements_Reference",
                table: "Remboursements",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Remboursements_VersionCommandeId",
                table: "Remboursements",
                column: "VersionCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nom",
                table: "Roles",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocksProduit_EntrepotId_ProduitId",
                table: "StocksProduit",
                columns: new[] { "EntrepotId", "ProduitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocksProduit_ProduitId",
                table: "StocksProduit",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAV_ClientId",
                table: "TicketsSAV",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAV_LigneCommandeId",
                table: "TicketsSAV",
                column: "LigneCommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAV_Reference",
                table: "TicketsSAV",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateurRoles_RoleId",
                table: "UtilisateurRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateurRoles_UtilisateurId",
                table: "UtilisateurRoles",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionsCommande_CommandeId_NumeroVersion",
                table: "VersionsCommande",
                columns: new[] { "CommandeId", "NumeroVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adresses");

            migrationBuilder.DropTable(
                name: "AdressesLivraisonCommande");

            migrationBuilder.DropTable(
                name: "AttributsProduit");

            migrationBuilder.DropTable(
                name: "Avoirs");

            migrationBuilder.DropTable(
                name: "Diagnostics");

            migrationBuilder.DropTable(
                name: "ImagesProduit");

            migrationBuilder.DropTable(
                name: "Interventions");

            migrationBuilder.DropTable(
                name: "JournauxAudit");

            migrationBuilder.DropTable(
                name: "LignesDevis");

            migrationBuilder.DropTable(
                name: "LignesPanier");

            migrationBuilder.DropTable(
                name: "MouvementsStock");

            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropTable(
                name: "PreuvesLivraison");

            migrationBuilder.DropTable(
                name: "Remboursements");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UtilisateurRoles");

            migrationBuilder.DropTable(
                name: "TicketsSAV");

            migrationBuilder.DropTable(
                name: "Paniers");

            migrationBuilder.DropTable(
                name: "StocksProduit");

            migrationBuilder.DropTable(
                name: "Livraisons");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "LignesCommande");

            migrationBuilder.DropTable(
                name: "Entrepots");

            migrationBuilder.DropTable(
                name: "Produits");

            migrationBuilder.DropTable(
                name: "VersionsCommande");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Marques");

            migrationBuilder.DropTable(
                name: "Commandes");

            migrationBuilder.DropTable(
                name: "Devis");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}
