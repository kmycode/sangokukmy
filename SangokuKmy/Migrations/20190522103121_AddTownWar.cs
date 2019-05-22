using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddTownWar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "town_war_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<uint>(
                name: "town_war_target_town_id",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "town_war_policy",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "town_war_target_town_id",
                table: "ai_country_managements");
        }
    }
}
