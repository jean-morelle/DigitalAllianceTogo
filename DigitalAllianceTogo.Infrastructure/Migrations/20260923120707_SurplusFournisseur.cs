using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SurplusFournisseur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EcartsReception",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuantiteCommandee = table.Column<int>(type: "integer", nullable: false),
                    QuantiteRecue = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DateConstat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConstateParId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateDecision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecideParId = table.Column<Guid>(type: "uuid", nullable: true),
                    MotifDecision = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProduitId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntrepotId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcartsReception", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EcartsReception_Entrepots_EntrepotId",
                        column: x => x.EntrepotId,
                        principalTable: "Entrepots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EcartsReception_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EcartsReception_EntrepotId",
                table: "EcartsReception",
                column: "EntrepotId");

            migrationBuilder.CreateIndex(
                name: "IX_EcartsReception_ProduitId",
                table: "EcartsReception",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_EcartsReception_Statut",
                table: "EcartsReception",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EcartsReception");
        }
    }
}
