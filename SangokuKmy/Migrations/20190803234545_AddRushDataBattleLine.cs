using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddRushDataBattleLine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_attacker_rush",
                table: "battle_log_lines");

            migrationBuilder.DropColumn(
                name: "is_defender_rush",
                table: "battle_log_lines");
        }
    }
}
