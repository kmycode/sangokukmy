using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddTownBuyCost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "takeover_defense_point",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "town_sub_building_extra_space",
                table: "town",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "takeover_defense_point",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "town_sub_building_extra_space",
                table: "scouted_town",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "takeover_defense_point",
                table: "town");

            migrationBuilder.DropColumn(
                name: "town_sub_building_extra_space",
                table: "town");

            migrationBuilder.DropColumn(
                name: "takeover_defense_point",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "town_sub_building_extra_space",
                table: "scouted_town");
        }
    }
}
