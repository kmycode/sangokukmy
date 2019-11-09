using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class UpgradeItemResourceType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "resource",
                table: "character_items",
                nullable: false,
                oldClrType: typeof(ushort));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ushort>(
                name: "resource",
                table: "character_items",
                nullable: false,
                oldClrType: typeof(int));
        }
    }
}
