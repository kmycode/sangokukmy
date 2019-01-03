using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryOverthrownFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_overthrown",
                table: "countries",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "overthrown_game_date",
                table: "countries",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_overthrown",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "overthrown_game_date",
                table: "countries");
        }
    }
}
