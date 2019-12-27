using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class RemoveCustomSoldierTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_soldier_types");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_soldier_types",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    archer = table.Column<short>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    common_soldier = table.Column<short>(nullable: false),
                    guard_1 = table.Column<short>(nullable: false),
                    guard_2 = table.Column<short>(nullable: false),
                    guard_3 = table.Column<short>(nullable: false),
                    guard_4 = table.Column<short>(nullable: false),
                    heavy_cavalry = table.Column<short>(nullable: false),
                    heavy_infantory = table.Column<short>(nullable: false),
                    intellect = table.Column<short>(nullable: false),
                    light_cavalry = table.Column<short>(nullable: false),
                    light_infantory = table.Column<short>(nullable: false),
                    light_intellect = table.Column<short>(nullable: false),
                    name = table.Column<string>(type: "varchar(32)", nullable: true),
                    repeating_crossbow = table.Column<short>(nullable: false),
                    research_cost = table.Column<short>(nullable: false),
                    rice_per_turn = table.Column<int>(nullable: false),
                    seiran = table.Column<short>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    strong_crossbow = table.Column<short>(nullable: false),
                    strong_guards = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_soldier_types", x => x.id);
                });
        }
    }
}
