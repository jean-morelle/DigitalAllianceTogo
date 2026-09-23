using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexUtilisateurRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UtilisateurRoles_UtilisateurId",
                table: "UtilisateurRoles");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateurRoles_UtilisateurId_RoleId",
                table: "UtilisateurRoles",
                columns: new[] { "UtilisateurId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UtilisateurRoles_UtilisateurId_RoleId",
                table: "UtilisateurRoles");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateurRoles_UtilisateurId",
                table: "UtilisateurRoles",
                column: "UtilisateurId");
        }
    }
}
