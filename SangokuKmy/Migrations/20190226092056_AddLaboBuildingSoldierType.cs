using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddLaboBuildingSoldierType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_soldier_types",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    preset = table.Column<short>(nullable: false),
                    is_conscript_disabled = table.Column<bool>(nullable: false),
                    name = table.Column<string>(type: "varchar(32)", nullable: true),
                    money = table.Column<short>(nullable: false),
                    rice_per_turn = table.Column<int>(nullable: false),
                    technology = table.Column<short>(nullable: false),
                    research_cost = table.Column<short>(nullable: false),
                    base_attack = table.Column<short>(nullable: false),
                    base_defend = table.Column<short>(nullable: false),
                    strong_attack = table.Column<short>(nullable: false),
                    strong_defend = table.Column<short>(nullable: false),
                    intellect_attack = table.Column<short>(nullable: false),
                    intellect_defend = table.Column<short>(nullable: false),
                    leadership_attack = table.Column<short>(nullable: false),
                    leadership_defend = table.Column<short>(nullable: false),
                    popularity_attack = table.Column<short>(nullable: false),
                    popularity_defend = table.Column<short>(nullable: false),
                    rush_probability = table.Column<short>(nullable: false),
                    rush_attack = table.Column<short>(nullable: false),
                    rush_defend = table.Column<short>(nullable: false),
                    rush_against_attack = table.Column<short>(nullable: false),
                    rush_against_defend = table.Column<short>(nullable: false),
                    continuous_probability = table.Column<short>(nullable: false),
                    continuous_attack = table.Column<short>(nullable: false),
                    continuous_defend = table.Column<short>(nullable: false),
                    wall_attack = table.Column<short>(nullable: false),
                    wall_defend = table.Column<short>(nullable: false),
                    through_defenders_probability = table.Column<short>(nullable: false),
                    recovery = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_soldier_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_researches",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    country_id = table.Column<uint>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    level = table.Column<short>(nullable: false),
                    progress = table.Column<int>(nullable: false),
                    progress_max = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_researches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scouted_buildings",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    town_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    value = table.Column<int>(nullable: false),
                    value_max = table.Column<int>(nullable: false),
                    scout_id = table.Column<uint>(nullable: false)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_soldier_types");

            migrationBuilder.DropTable(
                name: "country_researches");

            migrationBuilder.DropTable(
                name: "scouted_buildings");

            migrationBuilder.DropTable(
                name: "town_buildings");
        }
    }
}
