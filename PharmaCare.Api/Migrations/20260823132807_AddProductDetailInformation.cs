using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDetailInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "brand",
                table: "products",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "composition",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contraindications",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_of_origin",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dosage_form",
                table: "products",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer",
                table: "products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shelf_life",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "side_effects",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usage_instructions",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "brand",
                table: "products");

            migrationBuilder.DropColumn(
                name: "composition",
                table: "products");

            migrationBuilder.DropColumn(
                name: "contraindications",
                table: "products");

            migrationBuilder.DropColumn(
                name: "country_of_origin",
                table: "products");

            migrationBuilder.DropColumn(
                name: "dosage_form",
                table: "products");

            migrationBuilder.DropColumn(
                name: "manufacturer",
                table: "products");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "products");

            migrationBuilder.DropColumn(
                name: "shelf_life",
                table: "products");

            migrationBuilder.DropColumn(
                name: "side_effects",
                table: "products");

            migrationBuilder.DropColumn(
                name: "usage_instructions",
                table: "products");
        }
    }
}
