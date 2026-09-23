using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Lien = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DateLecture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatutEmail = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TentativesEmail = table.Column<int>(type: "integer", nullable: false),
                    DateEnvoiEmail = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErreurEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ClientId_DateCreation",
                table: "Notifications",
                columns: new[] { "ClientId", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StatutEmail",
                table: "Notifications",
                column: "StatutEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
