using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class RemoveAiCountryUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "border_unit_id",
                table: "ai_country_storategies");

            migrationBuilder.DropColumn(
                name: "main_unit_id",
                table: "ai_country_storategies");

            migrationBuilder.DropColumn(
                name: "unit_gather_policy",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "unit_policy",
                table: "ai_country_managements");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "border_unit_id",
                table: "ai_country_storategies",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "main_unit_id",
                table: "ai_country_storategies",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<short>(
                name: "unit_gather_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "unit_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
