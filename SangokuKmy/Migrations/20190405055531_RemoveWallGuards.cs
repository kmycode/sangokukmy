using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class RemoveWallGuards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "wallguard",
                table: "town");

            migrationBuilder.DropColumn(
                name: "wallguard_max",
                table: "town");

            migrationBuilder.DropColumn(
                name: "wallguard",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "wallguard_max",
                table: "scouted_town");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "wallguard",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wallguard_max",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wallguard",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wallguard_max",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);
        }
    }
}
