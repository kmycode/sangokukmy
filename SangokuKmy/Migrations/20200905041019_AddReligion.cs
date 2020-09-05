using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddReligion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "buddhism",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "confucianism",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "taoism",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "buddhism",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "confucianism",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "taoism",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "religion",
                table: "countries",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "religion",
                table: "characters",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "buddhism",
                table: "town");

            migrationBuilder.DropColumn(
                name: "confucianism",
                table: "town");

            migrationBuilder.DropColumn(
                name: "taoism",
                table: "town");

            migrationBuilder.DropColumn(
                name: "buddhism",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "confucianism",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "taoism",
                table: "scouted_town");

            migrationBuilder.DropColumn(
                name: "religion",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "religion",
                table: "characters");
        }
    }
}
