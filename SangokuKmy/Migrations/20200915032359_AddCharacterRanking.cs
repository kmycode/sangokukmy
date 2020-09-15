using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCharacterRanking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "battle_being_killed_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_broke_wall_size",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_continuous_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_dominate_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_killed_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_lost_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_scheme_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_won_count",
                table: "characters");

            migrationBuilder.CreateTable(
                name: "character_ranking",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    battle_won_count = table.Column<int>(nullable: false),
                    battle_lost_count = table.Column<int>(nullable: false),
                    battle_broke_wall_size = table.Column<int>(nullable: false),
                    battle_dominate_count = table.Column<int>(nullable: false),
                    battle_continuous_count = table.Column<int>(nullable: false),
                    battle_scheme_count = table.Column<int>(nullable: false),
                    battle_killed_count = table.Column<int>(nullable: false),
                    battle_being_killed_count = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_ranking", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_ranking");

            migrationBuilder.AddColumn<int>(
                name: "battle_being_killed_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_broke_wall_size",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_continuous_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_dominate_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_killed_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_lost_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_scheme_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_won_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);
        }
    }
}
