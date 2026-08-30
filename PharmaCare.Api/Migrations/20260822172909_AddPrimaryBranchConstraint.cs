using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimaryBranchConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_user_branches_user_id",
                table: "user_branches",
                column: "user_id",
                unique: true,
                filter: "is_primary = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_branches_user_id",
                table: "user_branches");
        }
    }
}
