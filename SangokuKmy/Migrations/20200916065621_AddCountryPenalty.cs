using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryPenalty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_war_penalty",
                table: "countries",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_war_penalty",
                table: "countries");
        }
    }
}
