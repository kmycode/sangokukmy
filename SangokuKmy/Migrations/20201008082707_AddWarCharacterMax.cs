using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddWarCharacterMax : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "insisted_country_character_max",
                table: "country_wars",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "requested_country_character_max",
                table: "country_wars",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "insisted_country_character_max",
                table: "country_wars");

            migrationBuilder.DropColumn(
                name: "requested_country_character_max",
                table: "country_wars");
        }
    }
}
