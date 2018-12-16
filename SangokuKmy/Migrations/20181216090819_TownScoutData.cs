using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class TownScoutData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scouted_town",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<byte>(nullable: false),
                    country_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    x = table.Column<short>(nullable: false),
                    y = table.Column<short>(nullable: false),
                    people = table.Column<int>(nullable: false),
                    agriculture = table.Column<int>(nullable: false),
                    agriculture_max = table.Column<int>(nullable: false),
                    commercial = table.Column<int>(nullable: false),
                    commercial_max = table.Column<int>(nullable: false),
                    technology = table.Column<int>(nullable: false),
                    technology_max = table.Column<int>(nullable: false),
                    wall = table.Column<int>(nullable: false),
                    wall_max = table.Column<int>(nullable: false),
                    wallguard = table.Column<int>(nullable: false),
                    wallguard_max = table.Column<int>(nullable: false),
                    security = table.Column<short>(nullable: false),
                    rice_price = table.Column<int>(nullable: false),
                    scouted_country_id = table.Column<uint>(nullable: false),
                    scouted_character_id = table.Column<uint>(nullable: false),
                    scout_method = table.Column<short>(nullable: false),
                    scouted_game_date_time = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scouted_town", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scouted_town");
        }
    }
}
