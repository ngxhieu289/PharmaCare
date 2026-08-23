using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDomainRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batches_products_product_id",
                table: "batches");

            migrationBuilder.DropForeignKey(
                name: "FK_branch_inventories_batches_batch_id",
                table: "branch_inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_products_product_id",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_category_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_order_items_product_id",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_branch_inventories_batch_id",
                table: "branch_inventories");

            migrationBuilder.DropIndex(
                name: "IX_branch_inventories_product_id",
                table: "branch_inventories");

            migrationBuilder.DropIndex(
                name: "IX_batches_product_id",
                table: "batches");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_order_amount",
                table: "vouchers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_value",
                table: "vouchers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "vat_rate",
                table: "products",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_vat_amount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "subtotal_before_vat",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "shipping_fee",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "vat_rate",
                table: "order_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "vat_amount",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total",
                table: "order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            // PostgreSQL không tự ép timestamp with time zone sang date. Chuyển theo UTC
            // để ngày sản xuất/hết hạn không phụ thuộc timezone của database session.
            migrationBuilder.Sql(
                "ALTER TABLE batches ALTER COLUMN mfg_date TYPE date USING (mfg_date AT TIME ZONE 'UTC')::date;");
            migrationBuilder.Sql(
                "ALTER TABLE batches ALTER COLUMN expiry_date TYPE date USING (expiry_date AT TIME ZONE 'UTC')::date;");

            migrationBuilder.AlterColumn<decimal>(
                name: "cost_price",
                table: "batches",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_batches_product_id_id",
                table: "batches",
                columns: new[] { "product_id", "id" });

            migrationBuilder.CreateTable(
                name: "user_branches",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_branches", x => new { x.user_id, x.branch_id });
                    table.ForeignKey(
                        name: "FK_user_branches_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_branches_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_assigned_customer_id",
                table: "vouchers",
                column: "assigned_customer_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_percentage",
                table: "vouchers",
                sql: "discount_type <> 'PERCENTAGE' OR discount_value <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_values",
                table: "vouchers",
                sql: "discount_value >= 0 AND min_order_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_unit_price",
                table: "products",
                sql: "unit_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_vat_rate",
                table: "products",
                sql: "vat_rate >= 0 AND vat_rate <= 100");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_customer_id",
                table: "prescriptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_prescriptions_pharmacist_id",
                table: "prescriptions",
                column: "pharmacist_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_branch_id",
                table: "orders",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_prescription_id",
                table: "orders",
                column: "prescription_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_amounts",
                table: "orders",
                sql: "subtotal_before_vat >= 0 AND total_vat_amount >= 0 AND shipping_fee >= 0 AND discount_amount >= 0 AND total_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id_batch_id",
                table: "order_items",
                columns: new[] { "product_id", "batch_id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_items_amounts",
                table: "order_items",
                sql: "unit_price >= 0 AND vat_amount >= 0 AND line_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_items_quantity",
                table: "order_items",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_items_vat_rate",
                table: "order_items",
                sql: "vat_rate >= 0 AND vat_rate <= 100");

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_inventories_product_id_batch_id",
                table: "branch_inventories",
                columns: new[] { "product_id", "batch_id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_branch_inventories_quantities",
                table: "branch_inventories",
                sql: "quantity_on_hand >= 0 AND reserved_quantity >= 0 AND reorder_level >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_branch_inventories_reserved",
                table: "branch_inventories",
                sql: "reserved_quantity <= quantity_on_hand");

            migrationBuilder.CreateIndex(
                name: "IX_batches_product_id_batch_number",
                table: "batches",
                columns: new[] { "product_id", "batch_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_batches_cost_price",
                table: "batches",
                sql: "cost_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_batches_dates",
                table: "batches",
                sql: "expiry_date >= mfg_date");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_branches_branch_id",
                table: "user_branches",
                column: "branch_id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_users_user_id",
                table: "audit_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_batches_products_product_id",
                table: "batches",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_branch_inventories_batches_product_id_batch_id",
                table: "branch_inventories",
                columns: new[] { "product_id", "batch_id" },
                principalTable: "batches",
                principalColumns: new[] { "product_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_categories_categories_parent_id",
                table: "categories",
                column: "parent_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_batches_product_id_batch_id",
                table: "order_items",
                columns: new[] { "product_id", "batch_id" },
                principalTable: "batches",
                principalColumns: new[] { "product_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_products_product_id",
                table: "order_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_branches_branch_id",
                table: "orders",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_prescriptions_prescription_id",
                table: "orders",
                column: "prescription_id",
                principalTable: "prescriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_users_customer_id",
                table: "orders",
                column: "customer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_users_customer_id",
                table: "prescriptions",
                column: "customer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prescriptions_users_pharmacist_id",
                table: "prescriptions",
                column: "pharmacist_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vouchers_users_assigned_customer_id",
                table: "vouchers",
                column: "assigned_customer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_users_user_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_batches_products_product_id",
                table: "batches");

            migrationBuilder.DropForeignKey(
                name: "FK_branch_inventories_batches_product_id_batch_id",
                table: "branch_inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_categories_categories_parent_id",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_batches_product_id_batch_id",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_products_product_id",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_branches_branch_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_prescriptions_prescription_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_customer_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_users_customer_id",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_prescriptions_users_pharmacist_id",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_category_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_vouchers_users_assigned_customer_id",
                table: "vouchers");

            migrationBuilder.DropTable(
                name: "user_branches");

            migrationBuilder.DropIndex(
                name: "IX_vouchers_assigned_customer_id",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_percentage",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_values",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_products_unit_price",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_products_vat_rate",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_customer_id",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_prescriptions_pharmacist_id",
                table: "prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_orders_branch_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_customer_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_prescription_id",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_amounts",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_items_product_id_batch_id",
                table: "order_items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_items_amounts",
                table: "order_items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_items_quantity",
                table: "order_items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_items_vat_rate",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_categories_parent_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_slug",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_branch_inventories_product_id_batch_id",
                table: "branch_inventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_branch_inventories_quantities",
                table: "branch_inventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_branch_inventories_reserved",
                table: "branch_inventories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_batches_product_id_id",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "IX_batches_product_id_batch_number",
                table: "batches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_batches_cost_price",
                table: "batches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_batches_dates",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_order_amount",
                table: "vouchers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_value",
                table: "vouchers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "vat_rate",
                table: "products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "products",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_vat_amount",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "subtotal_before_vat",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "shipping_fee",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "vat_rate",
                table: "order_items",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "vat_amount",
                table: "order_items",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "order_items",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total",
                table: "order_items",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.Sql(
                "ALTER TABLE batches ALTER COLUMN mfg_date TYPE timestamp with time zone USING mfg_date::timestamp AT TIME ZONE 'UTC';");
            migrationBuilder.Sql(
                "ALTER TABLE batches ALTER COLUMN expiry_date TYPE timestamp with time zone USING expiry_date::timestamp AT TIME ZONE 'UTC';");

            migrationBuilder.AlterColumn<decimal>(
                name: "cost_price",
                table: "batches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_inventories_batch_id",
                table: "branch_inventories",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_inventories_product_id",
                table: "branch_inventories",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_batches_product_id",
                table: "batches",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_batches_products_product_id",
                table: "batches",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_branch_inventories_batches_batch_id",
                table: "branch_inventories",
                column: "batch_id",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_products_product_id",
                table: "order_items",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
