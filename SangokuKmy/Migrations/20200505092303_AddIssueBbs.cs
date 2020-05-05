using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddIssueBbs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "issue_bbs_item_id",
                table: "mutes",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "issue_bbs_items",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    parent_id = table.Column<uint>(nullable: false),
                    account_id = table.Column<uint>(nullable: false),
                    title = table.Column<string>(type: "varchar(120)", nullable: true),
                    text = table.Column<string>(nullable: true),
                    written = table.Column<DateTime>(nullable: false),
                    last_modified = table.Column<DateTime>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    priority = table.Column<short>(nullable: false),
                    ApiPriority = table.Column<short>(nullable: false),
                    category = table.Column<short>(nullable: false),
                    ApiCategory = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issue_bbs_items", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_bbs_items");

            migrationBuilder.DropColumn(
                name: "issue_bbs_item_id",
                table: "mutes");
        }
    }
}
