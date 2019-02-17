using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class IpAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_use_debug_commands",
                table: "system_debug",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "password",
                table: "system_debug",
                type: "varchar(32)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_check_duplicate_entry",
                table: "system_debug",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "entry_hosts",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    ip_address = table.Column<string>(type: "varchar(128)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_hosts", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entry_hosts");

            migrationBuilder.DropColumn(
                name: "can_use_debug_commands",
                table: "system_debug");

            migrationBuilder.DropColumn(
                name: "password",
                table: "system_debug");

            migrationBuilder.DropColumn(
                name: "is_check_duplicate_entry",
                table: "system_debug");
        }
    }
}
