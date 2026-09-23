using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClientsIdentiteEtSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Clients",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nom",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Prenom",
                table: "Clients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaisonSociale",
                table: "Clients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Clients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Autre"); // canal inconnu pour les clients existants

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Clients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Clients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Particulier"); // une chaîne vide ne serait pas une valeur d'enum valide

            // Clients existants ayant un compte : on reprend leur identité depuis l'Utilisateur
            migrationBuilder.Sql(@"
                UPDATE ""Clients"" c
                SET ""Nom""       = u.""Nom"",
                    ""Prenom""    = NULLIF(u.""Prenom"", ''),
                    ""Telephone"" = u.""Telephone"",
                    ""Email""     = lower(u.""Email"")
                FROM ""Utilisateurs"" u
                WHERE c.""UtilisateurId"" = u.""Id"";");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Telephone",
                table: "Clients",
                column: "Telephone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clients_Telephone",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Nom",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Prenom",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RaisonSociale",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Clients");
        }
    }
}
