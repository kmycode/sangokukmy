using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class BattleLogLineRushToCommand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_attacker_rush",
                table: "battle_log_lines");

            migrationBuilder.DropColumn(
                name: "is_defender_rush",
                table: "battle_log_lines");

            migrationBuilder.AddColumn<short>(
                name: "attacker_command",
                table: "battle_log_lines",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "defender_command",
                table: "battle_log_lines",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attacker_command",
                table: "battle_log_lines");

            migrationBuilder.DropColumn(
                name: "defender_command",
                table: "battle_log_lines");

            migrationBuilder.AddColumn<bool>(
                name: "is_attacker_rush",
                table: "battle_log_lines",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_defender_rush",
                table: "battle_log_lines",
                nullable: false,
                defaultValue: false);
        }
    }
}
