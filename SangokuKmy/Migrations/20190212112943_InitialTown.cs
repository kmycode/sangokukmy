using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class InitialTown : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "people_max",
                table: "town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "reset_game_date_time",
                table: "system_data",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_next_period_beta",
                table: "system_data",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_waiting_reset",
                table: "system_data",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "people_max",
                table: "scouted_town",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "initial_town",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<byte>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    x = table.Column<short>(nullable: false),
                    y = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_initial_town", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "initial_town");

            migrationBuilder.DropColumn(
                name: "people_max",
                table: "town");

            migrationBuilder.DropColumn(
                name: "reset_game_date_time",
                table: "system_data");

            migrationBuilder.DropColumn(
                name: "is_next_period_beta",
                table: "system_data");

            migrationBuilder.DropColumn(
                name: "is_waiting_reset",
                table: "system_data");

            migrationBuilder.DropColumn(
                name: "people_max",
                table: "scouted_town");
        }
    }
}
