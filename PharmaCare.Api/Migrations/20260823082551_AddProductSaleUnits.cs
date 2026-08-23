using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSaleUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sale_quantity",
                table: "order_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "sale_unit_id",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sale_unit_name",
                table: "order_items",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Đơn vị");

            migrationBuilder.CreateTable(
                name: "product_sale_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    conversion_factor = table.Column<int>(type: "integer", nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_sale_units", x => x.id);
                    table.CheckConstraint("CK_product_sale_units_conversion", "conversion_factor > 0");
                    table.CheckConstraint("CK_product_sale_units_price", "sale_price >= 0");
                    table.ForeignKey(
                        name: "FK_product_sale_units_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_sale_unit_id",
                table: "order_items",
                column: "sale_unit_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_order_items_sale_quantity",
                table: "order_items",
                sql: "sale_quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_product_sale_units_product_id_unit_name",
                table: "product_sale_units",
                columns: new[] { "product_id", "unit_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_product_sale_units_sale_unit_id",
                table: "order_items",
                column: "sale_unit_id",
                principalTable: "product_sale_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_product_sale_units_sale_unit_id",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "product_sale_units");

            migrationBuilder.DropIndex(
                name: "IX_order_items_sale_unit_id",
                table: "order_items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_order_items_sale_quantity",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "sale_quantity",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "sale_unit_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "sale_unit_name",
                table: "order_items");
        }
    }
}
