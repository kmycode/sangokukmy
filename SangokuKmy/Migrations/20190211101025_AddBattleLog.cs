using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddBattleLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "battle_log_id",
                table: "map_logs",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "character_icons",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "battle_log_lines",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    battle_log_id = table.Column<uint>(nullable: false),
                    turn = table.Column<short>(nullable: false),
                    attacker_damage = table.Column<short>(nullable: false),
                    attacker_number = table.Column<short>(nullable: false),
                    defender_damage = table.Column<short>(nullable: false),
                    defender_number = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_log_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "battle_logs",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    town_id = table.Column<uint>(nullable: false),
                    attacker_character_id = table.Column<uint>(nullable: false),
                    defender_type = table.Column<short>(nullable: false),
                    defender_character_id = table.Column<uint>(nullable: false),
                    attacker_cache_id = table.Column<uint>(nullable: false),
                    defender_cache_id = table.Column<uint>(nullable: false),
                    maplog_id = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_caches",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    icon_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    country_id = table.Column<uint>(nullable: false),
                    strong = table.Column<short>(nullable: false),
                    intellect = table.Column<short>(nullable: false),
                    leadership = table.Column<short>(nullable: false),
                    popularity = table.Column<short>(nullable: false),
                    soldier_type = table.Column<short>(nullable: false),
                    soldier_number = table.Column<int>(nullable: false),
                    proficiency = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_caches", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_log_lines");

            migrationBuilder.DropTable(
                name: "battle_logs");

            migrationBuilder.DropTable(
                name: "character_caches");

            migrationBuilder.DropColumn(
                name: "battle_log_id",
                table: "map_logs");

            migrationBuilder.DropColumn(
                name: "is_available",
                table: "character_icons");
        }
    }
}
