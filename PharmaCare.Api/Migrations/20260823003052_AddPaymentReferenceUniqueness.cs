using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReferenceUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_external_reference",
                table: "payment_transactions",
                column: "external_reference",
                unique: true,
                filter: "external_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_external_reference",
                table: "payment_transactions");
        }
    }
}
