using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddFormationLevelAndBoost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "experience",
                table: "formations",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "level",
                table: "formations",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "status",
                table: "country_policies",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "formation_level",
                table: "character_caches",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "experience",
                table: "formations");

            migrationBuilder.DropColumn(
                name: "level",
                table: "formations");

            migrationBuilder.DropColumn(
                name: "status",
                table: "country_policies");

            migrationBuilder.DropColumn(
                name: "formation_level",
                table: "character_caches");
        }
    }
}
