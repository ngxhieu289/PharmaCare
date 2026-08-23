using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "prescriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE prescriptions SET branch_id = (SELECT id FROM branches WHERE is_active ORDER BY code LIMIT 1) WHERE branch_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "prescriptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "prescriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "prescriptions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "prescription_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_quantity = table.Column<int>(type: "integer", nullable: false),
                    dosage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    instructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prescription_items", x => x.id);
                    table.CheckConstraint("CK_prescription_items_quantity", "approved_quantity > 0");
                    table.ForeignKey(
                        name: "FK_prescription_items_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prescription_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_branch_id_status",
                table: "prescriptions",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_prescriptions_status",
                table: "prescriptions",
                sql: "status IN ('PENDING','APPROVED','REJECTED')");

            migrationBuilder.CreateIndex(
                name: "IX_prescription_items_prescription_id_product_id",
                table: "prescription_items",
                columns: new[] { "prescription_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prescription_items_product_id",
                table: "prescription_items",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_branches_branch_id",
                table: "prescriptions",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_branches_branch_id",
                table: "prescriptions");

            migrationBuilder.DropTable(
                name: "prescription_items");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_branch_id_status",
                table: "prescriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_prescriptions_status",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "prescriptions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "prescriptions");
        }
    }
}
