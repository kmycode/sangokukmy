using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class CountryMessageAddtionalData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "writer_character_id",
                table: "country_messages",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<short>(
                name: "writer_post",
                table: "country_messages",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "writer_character_id",
                table: "country_messages");

            migrationBuilder.DropColumn(
                name: "writer_post",
                table: "country_messages");
        }
    }
}
