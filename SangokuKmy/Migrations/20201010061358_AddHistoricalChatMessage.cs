using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddHistoricalChatMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historical_chat_messages",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    history_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    type_data = table.Column<uint>(nullable: false),
                    type_data_2 = table.Column<uint>(nullable: false),
                    message = table.Column<string>(nullable: true),
                    posted = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_chat_messages", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_chat_messages");
        }
    }
}
