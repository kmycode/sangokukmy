using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryScouter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "country_building",
                table: "town");

            migrationBuilder.DropColumn(
                name: "country_building_value",
                table: "town");

            migrationBuilder.DropColumn(
                name: "country_laboratory",
                table: "town");

            migrationBuilder.DropColumn(
                name: "country_laboratory_value",
                table: "town");

            migrationBuilder.DropColumn(
                name: "country_building",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "country_building_value",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "country_laboratory",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "country_laboratory_value",
                table: "scouted_town");

            migrationBuilder.CreateTable(
                name: "country_scouters",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    country_id = table.Column<uint>(nullable: false),
                    town_id = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_scouters", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_scouters");

            migrationBuilder.AddColumn<short>(
                name: "country_building",
                table: "town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "country_building_value",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "country_laboratory",
                table: "town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "country_laboratory_value",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "country_building",
                table: "scouted_town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "country_building_value",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "country_laboratory",
                table: "scouted_town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "country_laboratory_value",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);
        }
    }
}
