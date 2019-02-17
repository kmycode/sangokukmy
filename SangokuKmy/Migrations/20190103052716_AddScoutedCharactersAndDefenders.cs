using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddScoutedCharactersAndDefenders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scouted_characters",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    scout_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    soldier_type = table.Column<short>(nullable: false),
                    soldier_number = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scouted_characters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scouted_defenders",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    scout_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    soldier_type = table.Column<short>(nullable: false),
                    soldier_number = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scouted_defenders", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scouted_characters");

            migrationBuilder.DropTable(
                name: "scouted_defenders");
        }
    }
}
