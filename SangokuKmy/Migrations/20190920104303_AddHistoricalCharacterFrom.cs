using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddHistoricalCharacterFrom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ai_type",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "from",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_type",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "from",
                table: "historical_characters");
        }
    }
}
