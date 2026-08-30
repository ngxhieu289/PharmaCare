using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLedgerAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "branch_inventories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transactions", x => x.id);
                    table.CheckConstraint("CK_inventory_transactions_balance", "balance_after >= 0");
                    table.CheckConstraint("CK_inventory_transactions_quantity", "quantity <> 0");
                    table.CheckConstraint("CK_inventory_transactions_type", "transaction_type IN ('IMPORT','ADJUST_IN','ADJUST_OUT','TRANSFER_IN','TRANSFER_OUT','RESERVE','RELEASE','SALE','RETURN')");
                    table.ForeignKey(
                        name: "FK_inventory_transactions_batches_product_id_batch_id",
                        columns: x => new { x.product_id, x.batch_id },
                        principalTable: "batches",
                        principalColumns: new[] { "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transactions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_branch_id_created_at",
                table: "inventory_transactions",
                columns: new[] { "branch_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_created_by",
                table: "inventory_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transactions_product_id_batch_id",
                table: "inventory_transactions",
                columns: new[] { "product_id", "batch_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_transactions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "branch_inventories");
        }
    }
}
