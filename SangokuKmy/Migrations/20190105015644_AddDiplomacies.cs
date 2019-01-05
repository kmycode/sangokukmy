using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddDiplomacies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "country_alliances",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status = table.Column<short>(nullable: false),
                    requested_country_id = table.Column<uint>(nullable: false),
                    insisted_country_id = table.Column<uint>(nullable: false),
                    is_public = table.Column<bool>(nullable: false),
                    breaking_delay = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_alliances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_wars",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status = table.Column<short>(nullable: false),
                    requested_country_id = table.Column<uint>(nullable: false),
                    insisted_country_id = table.Column<uint>(nullable: false),
                    requested_stop_country_id = table.Column<uint>(nullable: false),
                    start_game_date = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_wars", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_alliances");

            migrationBuilder.DropTable(
                name: "country_wars");
        }
    }
}
