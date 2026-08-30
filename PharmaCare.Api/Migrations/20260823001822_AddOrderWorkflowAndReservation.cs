using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderWorkflowAndReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_branch_id",
                table: "orders");

            migrationBuilder.AddColumn<string>(
                name: "recipient_name",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_phone",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "order_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_status_histories_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_status_histories_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_branch_id_status_created_at",
                table: "orders",
                columns: new[] { "branch_id", "status", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_pickup",
                table: "orders",
                sql: "pickup_type IN ('SHIPPING','STORE_PICKUP')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_status",
                table: "orders",
                sql: "status IN ('PENDING','CONFIRMED','COMPLETED','CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_type",
                table: "orders",
                sql: "order_type IN ('ONLINE','POS')");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_histories_changed_by",
                table: "order_status_histories",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_histories_order_id",
                table: "order_status_histories",
                column: "order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_status_histories");

            migrationBuilder.DropIndex(
                name: "IX_orders_branch_id_status_created_at",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_pickup",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_status",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_type",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "recipient_name",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "recipient_phone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_address",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "version",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_branch_id",
                table: "orders",
                column: "branch_id");
        }
    }
}
