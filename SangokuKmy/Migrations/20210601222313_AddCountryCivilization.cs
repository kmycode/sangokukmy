using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryCivilization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_may_be_bought",
                table: "town");

            migrationBuilder.DropColumn(
                name: "takeover_defense_point",
                table: "town");

            migrationBuilder.DropColumn(
                name: "takeover_defense_point",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "policy",
                table: "countries");

            migrationBuilder.AddColumn<short>(
                name: "civilization",
                table: "countries",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "civilization",
                table: "countries");

            migrationBuilder.AddColumn<bool>(
                name: "is_may_be_bought",
                table: "town",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "takeover_defense_point",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "takeover_defense_point",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "policy",
                table: "countries",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
