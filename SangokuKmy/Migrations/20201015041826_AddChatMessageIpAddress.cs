using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddChatMessageIpAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "historical_chat_messages",
                type: "varchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "char_messages",
                type: "varchar(128)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "historical_chat_messages");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "char_messages");
        }
    }
}
