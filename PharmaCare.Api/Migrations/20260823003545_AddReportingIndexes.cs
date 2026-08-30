using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action_created_at",
                table: "audit_logs",
                columns: new[] { "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_name_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_action_created_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_entity_name_entity_id",
                table: "audit_logs");
        }
    }
}
