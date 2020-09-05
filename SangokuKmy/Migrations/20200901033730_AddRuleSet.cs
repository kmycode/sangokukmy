using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddRuleSet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "rule_set",
                table: "system_data",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "rule_set_after_next_period",
                table: "system_data",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "rule_set_next_period",
                table: "system_data",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rule_set",
                table: "system_data");

            migrationBuilder.DropColumn(
                name: "rule_set_after_next_period",
                table: "system_data");

            migrationBuilder.DropColumn(
                name: "rule_set_next_period",
                table: "system_data");
        }
    }
}
