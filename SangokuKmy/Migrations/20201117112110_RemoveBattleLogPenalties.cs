using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class RemoveBattleLogPenalties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_attacker_penalty",
                table: "battle_logs");

            migrationBuilder.DropColumn(
                name: "is_defender_penalty",
                table: "battle_logs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_attacker_penalty",
                table: "battle_logs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_defender_penalty",
                table: "battle_logs",
                nullable: false,
                defaultValue: false);
        }
    }
}
