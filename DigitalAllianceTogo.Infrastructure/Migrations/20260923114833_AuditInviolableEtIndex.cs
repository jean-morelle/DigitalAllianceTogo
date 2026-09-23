using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAllianceTogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditInviolableEtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Règle absolue (§45) : le journal d'audit est en ajout seul, y compris pour
            // quelqu'un qui accéderait directement à la base (UPDATE / DELETE / TRUNCATE refusés).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION journal_audit_inviolable() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Le journal d''audit est inviolable : % interdit', TG_OP;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER tr_journal_audit_sans_modification
                    BEFORE UPDATE OR DELETE ON "JournauxAudit"
                    FOR EACH ROW EXECUTE FUNCTION journal_audit_inviolable();

                CREATE TRIGGER tr_journal_audit_sans_vidage
                    BEFORE TRUNCATE ON "JournauxAudit"
                    FOR EACH STATEMENT EXECUTE FUNCTION journal_audit_inviolable();
                """);
            migrationBuilder.CreateIndex(
                name: "IX_TicketsSAV_Statut",
                table: "TicketsSAV",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_Statut_DateConfirmation",
                table: "Paiements",
                columns: new[] { "Statut", "DateConfirmation" });

            migrationBuilder.CreateIndex(
                name: "IX_Livraisons_Statut",
                table: "Livraisons",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_JournauxAudit_DateAction",
                table: "JournauxAudit",
                column: "DateAction");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_Statut",
                table: "Devis",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Commandes_Statut",
                table: "Commandes",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS tr_journal_audit_sans_vidage ON "JournauxAudit";
                DROP TRIGGER IF EXISTS tr_journal_audit_sans_modification ON "JournauxAudit";
                DROP FUNCTION IF EXISTS journal_audit_inviolable();
                """);
            migrationBuilder.DropIndex(
                name: "IX_TicketsSAV_Statut",
                table: "TicketsSAV");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_Statut_DateConfirmation",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Livraisons_Statut",
                table: "Livraisons");

            migrationBuilder.DropIndex(
                name: "IX_JournauxAudit_DateAction",
                table: "JournauxAudit");

            migrationBuilder.DropIndex(
                name: "IX_Devis_Statut",
                table: "Devis");

            migrationBuilder.DropIndex(
                name: "IX_Commandes_Statut",
                table: "Commandes");
        }
    }
}
