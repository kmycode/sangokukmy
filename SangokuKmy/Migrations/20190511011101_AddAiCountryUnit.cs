using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddAiCountryUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "border_unit_id",
                table: "ai_country_storategies",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "is_defend_force",
                table: "ai_country_storategies",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "main_unit_id",
                table: "ai_country_storategies",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<short>(
                name: "character_size",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "force_defend_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "is_policy_second",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<uint>(
                name: "virtual_enemy_country_id",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<short>(
                name: "war_start_date_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "war_target_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "border_unit_id",
                table: "ai_country_storategies");

            migrationBuilder.DropColumn(
                name: "is_defend_force",
                table: "ai_country_storategies");

            migrationBuilder.DropColumn(
                name: "main_unit_id",
                table: "ai_country_storategies");

            migrationBuilder.DropColumn(
                name: "character_size",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "force_defend_policy",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "is_policy_second",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "unit_gather_policy",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "unit_policy",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "virtual_enemy_country_id",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "war_start_date_policy",
                table: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "war_target_policy",
                table: "ai_country_managements");
        }
    }
}
