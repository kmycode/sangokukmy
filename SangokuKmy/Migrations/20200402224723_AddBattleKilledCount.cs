using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddBattleKilledCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "battle_being_killed_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_killed_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_scheme_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "battle_being_killed_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_killed_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_scheme_count",
                table: "characters");
        }
    }
}
