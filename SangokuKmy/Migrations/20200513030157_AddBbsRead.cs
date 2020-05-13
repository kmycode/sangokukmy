using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddBbsRead : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "last_country_bbs_id",
                table: "char_message_read",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "last_global_bbs_id",
                table: "char_message_read",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_country_bbs_id",
                table: "char_message_read");

            migrationBuilder.DropColumn(
                name: "last_global_bbs_id",
                table: "char_message_read");
        }
    }
}
