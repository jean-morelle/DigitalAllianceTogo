using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NumerosPaiement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomBeneficiairePaiement",
                table: "ParametresEntreprise",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroFlooz",
                table: "ParametresEntreprise",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroTMoney",
                table: "ParametresEntreprise",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ParametresEntreprise",
                keyColumn: "Id",
                keyValue: new Guid("5e1d7a3c-0b7e-4c1a-9a51-2f7c4d9e8a01"),
                columns: new[] { "NomBeneficiairePaiement", "NumeroFlooz", "NumeroTMoney" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomBeneficiairePaiement",
                table: "ParametresEntreprise");

            migrationBuilder.DropColumn(
                name: "NumeroFlooz",
                table: "ParametresEntreprise");

            migrationBuilder.DropColumn(
                name: "NumeroTMoney",
                table: "ParametresEntreprise");
        }
    }
}
