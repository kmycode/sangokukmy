using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class TownScoutData_ScoutedTownId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "scouted_town_id",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scouted_town_id",
                table: "scouted_town");
        }
    }
}
