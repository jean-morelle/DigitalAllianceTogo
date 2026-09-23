using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignementCahierDesCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantiteDefectueuse",
                table: "StocksProduit",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuantiteEnTransit",
                table: "StocksProduit",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmeParId",
                table: "Paiements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConfirmation",
                table: "Paiements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreuveUrl",
                table: "Paiements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceExterne",
                table: "Paiements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifEchec",
                table: "Livraisons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reserve",
                table: "Livraisons",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreeParId",
                table: "Devis",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValidation",
                table: "Devis",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValideParId",
                table: "Devis",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParametresEntreprise",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeuilRemiseCommercialPourcent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SeuilAugmentationModificationPourcent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DelaiExpirationPaiementHeures = table.Column<int>(type: "integer", nullable: false),
                    DureeValiditeDevisJours = table.Column<int>(type: "integer", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametresEntreprise", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ParametresEntreprise",
                columns: new[] { "Id", "DateModification", "DelaiExpirationPaiementHeures", "DureeValiditeDevisJours", "SeuilAugmentationModificationPourcent", "SeuilRemiseCommercialPourcent" },
                values: new object[] { new Guid("5e1d7a3c-0b7e-4c1a-9a51-2f7c4d9e8a01"), new DateTime(2026, 9, 23, 0, 0, 0, 0, DateTimeKind.Utc), 48, 15, 10m, 10m });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ConfirmeParId",
                table: "Paiements",
                column: "ConfirmeParId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_CreeParId",
                table: "Devis",
                column: "CreeParId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_ValideParId",
                table: "Devis",
                column: "ValideParId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devis_Utilisateurs_CreeParId",
                table: "Devis",
                column: "CreeParId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devis_Utilisateurs_ValideParId",
                table: "Devis",
                column: "ValideParId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Paiements_Utilisateurs_ConfirmeParId",
                table: "Paiements",
                column: "ConfirmeParId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devis_Utilisateurs_CreeParId",
                table: "Devis");

            migrationBuilder.DropForeignKey(
                name: "FK_Devis_Utilisateurs_ValideParId",
                table: "Devis");

            migrationBuilder.DropForeignKey(
                name: "FK_Paiements_Utilisateurs_ConfirmeParId",
                table: "Paiements");

            migrationBuilder.DropTable(
                name: "ParametresEntreprise");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ConfirmeParId",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Devis_CreeParId",
                table: "Devis");

            migrationBuilder.DropIndex(
                name: "IX_Devis_ValideParId",
                table: "Devis");

            migrationBuilder.DropColumn(
                name: "QuantiteDefectueuse",
                table: "StocksProduit");

            migrationBuilder.DropColumn(
                name: "QuantiteEnTransit",
                table: "StocksProduit");

            migrationBuilder.DropColumn(
                name: "ConfirmeParId",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "DateConfirmation",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "PreuveUrl",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "ReferenceExterne",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MotifEchec",
                table: "Livraisons");

            migrationBuilder.DropColumn(
                name: "Reserve",
                table: "Livraisons");

            migrationBuilder.DropColumn(
                name: "CreeParId",
                table: "Devis");

            migrationBuilder.DropColumn(
                name: "DateValidation",
                table: "Devis");

            migrationBuilder.DropColumn(
                name: "ValideParId",
                table: "Devis");
        }
    }
}
