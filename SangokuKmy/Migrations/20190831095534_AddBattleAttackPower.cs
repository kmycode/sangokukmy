using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddBattleAttackPower : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attacker_attack_power",
                table: "battle_logs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "defender_attack_power",
                table: "battle_logs",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attacker_attack_power",
                table: "battle_logs");

            migrationBuilder.DropColumn(
                name: "defender_attack_power",
                table: "battle_logs");
        }
    }
}
