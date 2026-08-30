using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherAndPaymentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_values",
                table: "vouchers");

            migrationBuilder.AddColumn<decimal>(
                name: "max_discount_amount",
                table: "vouchers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "per_customer_limit",
                table: "vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "usage_limit",
                table: "vouchers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "used_count",
                table: "vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_from",
                table: "vouchers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_until",
                table: "vouchers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "vouchers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    external_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.id);
                    table.CheckConstraint("CK_payment_transactions_amount", "amount > 0");
                    table.CheckConstraint("CK_payment_transactions_method", "method IN ('COD','VIETQR','CASH_POS')");
                    table.CheckConstraint("CK_payment_transactions_status", "status = 'SUCCEEDED'");
                    table.CheckConstraint("CK_payment_transactions_type", "transaction_type IN ('PAYMENT','REFUND')");
                    table.ForeignKey(
                        name: "FK_payment_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_transactions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_usages", x => x.id);
                    table.CheckConstraint("CK_voucher_usages_amount", "discount_amount > 0");
                    table.CheckConstraint("CK_voucher_usages_status", "status IN ('REDEEMED','REVERSED')");
                    table.ForeignKey(
                        name: "FK_voucher_usages_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_voucher_usages_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_dates",
                table: "vouchers",
                sql: "valid_until IS NULL OR valid_until > valid_from");

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_limits",
                table: "vouchers",
                sql: "per_customer_limit > 0 AND used_count >= 0 AND (usage_limit IS NULL OR usage_limit > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_type",
                table: "vouchers",
                sql: "discount_type IN ('FIXED_AMOUNT','PERCENTAGE')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_values",
                table: "vouchers",
                sql: "discount_value > 0 AND min_order_amount >= 0 AND (max_discount_amount IS NULL OR max_discount_amount > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_payment_method",
                table: "orders",
                sql: "payment_method IN ('COD','VIETQR','CASH_POS')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_payment_status",
                table: "orders",
                sql: "payment_status IN ('UNPAID','PAID','REFUNDED')");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_created_by",
                table: "payment_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_order_id_created_at",
                table: "payment_transactions",
                columns: new[] { "order_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_customer_id",
                table: "voucher_usages",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_order_id",
                table: "voucher_usages",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_voucher_id_customer_id_status",
                table: "voucher_usages",
                columns: new[] { "voucher_id", "customer_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "voucher_usages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_dates",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_limits",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_type",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_vouchers_values",
                table: "vouchers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_payment_method",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_payment_status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "max_discount_amount",
                table: "vouchers");

            migrationBuilder.DropColumn(
                name: "per_customer_limit",
                table: "vouchers");

            migrationBuilder.DropColumn(
                name: "usage_limit",
                table: "vouchers");

            migrationBuilder.DropColumn(
                name: "used_count",
                table: "vouchers");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "vouchers");

            migrationBuilder.DropColumn(
                name: "valid_until",
                table: "vouchers");

            migrationBuilder.DropColumn(
                name: "version",
                table: "vouchers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_vouchers_values",
                table: "vouchers",
                sql: "discount_value >= 0 AND min_order_amount >= 0");
        }
    }
}
