using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class DebugData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_debug",
                table: "system_data",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "system_debug",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updatable_game_date_time = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_debug", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_debug");

            migrationBuilder.DropColumn(
                name: "is_debug",
                table: "system_data");
        }
    }
}
