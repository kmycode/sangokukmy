using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryCommanderWriterInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "writer_character_id",
                table: "country_commanders",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<short>(
                name: "writer_post",
                table: "country_commanders",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "writer_character_id",
                table: "country_commanders");

            migrationBuilder.DropColumn(
                name: "writer_post",
                table: "country_commanders");
        }
    }
}
