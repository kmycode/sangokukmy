using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class RemoveTownBuildingTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scouted_buildings");

            migrationBuilder.DropTable(
                name: "town_buildings");

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
                name: "town_building",
                table: "town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "town_building_value",
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

            migrationBuilder.AddColumn<short>(
                name: "town_building",
                table: "scouted_town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "town_building_value",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "town_building",
                table: "town");

            migrationBuilder.DropColumn(
                name: "town_building_value",
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

            migrationBuilder.DropColumn(
                name: "town_building",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "town_building_value",
                table: "scouted_town");

            migrationBuilder.CreateTable(
                name: "scouted_buildings",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    scout_id = table.Column<uint>(nullable: false),
                    town_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    value = table.Column<int>(nullable: false),
                    value_max = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scouted_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "town_buildings",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    town_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    value = table.Column<int>(nullable: false),
                    value_max = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_town_buildings", x => x.id);
                });
        }
    }
}
