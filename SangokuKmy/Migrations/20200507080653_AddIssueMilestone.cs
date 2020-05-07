using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddIssueMilestone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "priority",
                table: "issue_bbs_items",
                newName: "period");

            migrationBuilder.AddColumn<short>(
                name: "beta_version",
                table: "issue_bbs_items",
                nullable: false,
                defaultValue: (short)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "beta_version",
                table: "issue_bbs_items");

            migrationBuilder.RenameColumn(
                name: "period",
                table: "issue_bbs_items",
                newName: "priority");
        }
    }
}
