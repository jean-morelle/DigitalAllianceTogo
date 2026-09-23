using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaiementsEtReservationStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "StocksProduit",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "MotifRejet",
                table: "Paiements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommandeId",
                table: "MouvementsStock",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UtilisateurId",
                table: "JournauxAudit",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Commandes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ReferenceExterne",
                table: "Paiements",
                column: "ReferenceExterne");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_CommandeId",
                table: "MouvementsStock",
                column: "CommandeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MouvementsStock_Commandes_CommandeId",
                table: "MouvementsStock",
                column: "CommandeId",
                principalTable: "Commandes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MouvementsStock_Commandes_CommandeId",
                table: "MouvementsStock");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ReferenceExterne",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_CommandeId",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "StocksProduit");

            migrationBuilder.DropColumn(
                name: "MotifRejet",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "CommandeId",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Commandes");

            migrationBuilder.AlterColumn<Guid>(
                name: "UtilisateurId",
                table: "JournauxAudit",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
