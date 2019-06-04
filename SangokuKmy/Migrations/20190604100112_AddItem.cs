using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "suzerain_country_id",
                table: "countries",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "from",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "character_items",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<short>(nullable: false),
                    status = table.Column<short>(nullable: false),
                    town_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_items", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_items");

            migrationBuilder.DropColumn(
                name: "suzerain_country_id",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "from",
                table: "characters");
        }
    }
}
