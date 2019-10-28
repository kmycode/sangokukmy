using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddTownSubBuilding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scouted_sub_buildings",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    scout_id = table.Column<uint>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    type = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scouted_sub_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "town_sub_buildings",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    town_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    status_finish_game_date_time = table.Column<int>(nullable: false),
                    type = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_town_sub_buildings", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scouted_sub_buildings");

            migrationBuilder.DropTable(
                name: "town_sub_buildings");
        }
    }
}
