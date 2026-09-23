using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModificationCommandeVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreeParId",
                table: "VersionsCommande",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateReponse",
                table: "VersionsCommande",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifRefus",
                table: "VersionsCommande",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Regularisation",
                table: "VersionsCommande",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Statut",
                table: "VersionsCommande",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Acceptee"); // versions existantes : issues du devis, acceptées

            migrationBuilder.AddColumn<Guid>(
                name: "ValideParId",
                table: "VersionsCommande",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionsCommande_Statut",
                table: "VersionsCommande",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VersionsCommande_Statut",
                table: "VersionsCommande");

            migrationBuilder.DropColumn(
                name: "CreeParId",
                table: "VersionsCommande");

            migrationBuilder.DropColumn(
                name: "DateReponse",
                table: "VersionsCommande");

            migrationBuilder.DropColumn(
                name: "MotifRefus",
                table: "VersionsCommande");

            migrationBuilder.DropColumn(
                name: "Regularisation",
                table: "VersionsCommande");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "VersionsCommande");

            migrationBuilder.DropColumn(
                name: "ValideParId",
                table: "VersionsCommande");
        }
    }
}
