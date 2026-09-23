using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaiementParAvoir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AvoirId",
                table: "Paiements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantUtilise",
                table: "Avoirs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Avoirs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_AvoirId",
                table: "Paiements",
                column: "AvoirId");

            migrationBuilder.AddForeignKey(
                name: "FK_Paiements_Avoirs_AvoirId",
                table: "Paiements",
                column: "AvoirId",
                principalTable: "Avoirs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paiements_Avoirs_AvoirId",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_AvoirId",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "AvoirId",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantUtilise",
                table: "Avoirs");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Avoirs");
        }
    }
}
