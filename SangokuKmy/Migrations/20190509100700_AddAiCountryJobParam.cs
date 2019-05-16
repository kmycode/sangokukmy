using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddAiCountryJobParam : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "war_style",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<uint>(
                name: "defender_id",
                table: "ai_battle_histories",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "rest_defender_count",
                table: "ai_battle_histories",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "town_type",
                table: "ai_battle_histories",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "war_style",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "defender_id",
                table: "ai_battle_histories");

            migrationBuilder.DropColumn(
                name: "rest_defender_count",
                table: "ai_battle_histories");

            migrationBuilder.DropColumn(
                name: "town_type",
                table: "ai_battle_histories");
        }
    }
}
