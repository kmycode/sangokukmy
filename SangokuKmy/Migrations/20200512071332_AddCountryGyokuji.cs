using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryGyokuji : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "suzerain_country_id",
                table: "countries");

            migrationBuilder.AddColumn<short>(
                name: "gyokuji_status",
                table: "countries",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "gyokuji_game_date",
                table: "countries",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gyokuji_status",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "gyokuji_game_date",
                table: "countries");

            migrationBuilder.AddColumn<uint>(
                name: "suzerain_country_id",
                table: "countries",
                nullable: false,
                defaultValue: 0u);
        }
    }
}
