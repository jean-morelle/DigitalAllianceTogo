using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DevisWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentaireClient",
                table: "Devis",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommentaireInterne",
                table: "Devis",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Devis",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentaireClient",
                table: "Devis");

            migrationBuilder.DropColumn(
                name: "CommentaireInterne",
                table: "Devis");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Devis");
        }
    }
}
