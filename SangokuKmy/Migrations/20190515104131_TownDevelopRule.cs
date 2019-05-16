using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class TownDevelopRule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "develop_town_id",
                table: "ai_country_storategies",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<short>(
                name: "develop_style",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "develop_town_id",
                table: "ai_country_storategies");

            migrationBuilder.DropColumn(
                name: "develop_style",
                table: "ai_country_managements");
        }
    }
}
