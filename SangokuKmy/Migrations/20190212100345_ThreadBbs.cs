using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class ThreadBbs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "thread_bbs_item",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<short>(nullable: false),
                    parent_id = table.Column<uint>(nullable: false),
                    country_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    character_icon_id = table.Column<uint>(nullable: false),
                    title = table.Column<string>(nullable: true),
                    text = table.Column<string>(nullable: true),
                    written = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thread_bbs_item", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thread_bbs_item");
        }
    }
}
