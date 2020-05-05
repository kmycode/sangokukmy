using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class FixIssueBbs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiCategory",
                table: "issue_bbs_items");

            migrationBuilder.DropColumn(
                name: "ApiPriority",
                table: "issue_bbs_items");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "ApiCategory",
                table: "issue_bbs_items",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "ApiPriority",
                table: "issue_bbs_items",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
