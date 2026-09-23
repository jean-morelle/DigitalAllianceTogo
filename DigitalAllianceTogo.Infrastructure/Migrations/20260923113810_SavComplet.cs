using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SavComplet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AncienProduitReceptionne",
                table: "TicketsSAV",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCloture",
                table: "TicketsSAV",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Decision",
                table: "TicketsSAV",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantite",
                table: "TicketsSAV",
                type: "integer",
                nullable: false,
                defaultValue: 1); // tickets existants : au moins une unité concernée

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "TicketsSAV",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicienId",
                table: "TicketsSAV",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "TicketsSAV",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketSAVId",
                table: "Remboursements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketSAVId",
                table: "MouvementsStock",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketSAVId",
                table: "Livraisons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketSAVId",
                table: "Avoirs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAV_TechnicienId",
                table: "TicketsSAV",
                column: "TechnicienId");

            migrationBuilder.CreateIndex(
                name: "IX_Remboursements_TicketSAVId",
                table: "Remboursements",
                column: "TicketSAVId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_TicketSAVId",
                table: "MouvementsStock",
                column: "TicketSAVId");

            migrationBuilder.CreateIndex(
                name: "IX_Livraisons_TicketSAVId",
                table: "Livraisons",
                column: "TicketSAVId");

            migrationBuilder.CreateIndex(
                name: "IX_Avoirs_TicketSAVId",
                table: "Avoirs",
                column: "TicketSAVId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avoirs_TicketsSAV_TicketSAVId",
                table: "Avoirs",
                column: "TicketSAVId",
                principalTable: "TicketsSAV",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Livraisons_TicketsSAV_TicketSAVId",
                table: "Livraisons",
                column: "TicketSAVId",
                principalTable: "TicketsSAV",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MouvementsStock_TicketsSAV_TicketSAVId",
                table: "MouvementsStock",
                column: "TicketSAVId",
                principalTable: "TicketsSAV",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Remboursements_TicketsSAV_TicketSAVId",
                table: "Remboursements",
                column: "TicketSAVId",
                principalTable: "TicketsSAV",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketsSAV_Utilisateurs_TechnicienId",
                table: "TicketsSAV",
                column: "TechnicienId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avoirs_TicketsSAV_TicketSAVId",
                table: "Avoirs");

            migrationBuilder.DropForeignKey(
                name: "FK_Livraisons_TicketsSAV_TicketSAVId",
                table: "Livraisons");

            migrationBuilder.DropForeignKey(
                name: "FK_MouvementsStock_TicketsSAV_TicketSAVId",
                table: "MouvementsStock");

            migrationBuilder.DropForeignKey(
                name: "FK_Remboursements_TicketsSAV_TicketSAVId",
                table: "Remboursements");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketsSAV_Utilisateurs_TechnicienId",
                table: "TicketsSAV");

            migrationBuilder.DropIndex(
                name: "IX_TicketsSAV_TechnicienId",
                table: "TicketsSAV");

            migrationBuilder.DropIndex(
                name: "IX_Remboursements_TicketSAVId",
                table: "Remboursements");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_TicketSAVId",
                table: "MouvementsStock");

            migrationBuilder.DropIndex(
                name: "IX_Livraisons_TicketSAVId",
                table: "Livraisons");

            migrationBuilder.DropIndex(
                name: "IX_Avoirs_TicketSAVId",
                table: "Avoirs");

            migrationBuilder.DropColumn(
                name: "AncienProduitReceptionne",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "DateCloture",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "Decision",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "Quantite",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "TechnicienId",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "TicketsSAV");

            migrationBuilder.DropColumn(
                name: "TicketSAVId",
                table: "Remboursements");

            migrationBuilder.DropColumn(
                name: "TicketSAVId",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "TicketSAVId",
                table: "Livraisons");

            migrationBuilder.DropColumn(
                name: "TicketSAVId",
                table: "Avoirs");
        }
    }
}
