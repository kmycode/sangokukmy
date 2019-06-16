using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddSkillAndDelayCommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "skill_point",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "character_skills",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<short>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    status = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delay_effects",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    town_id = table.Column<uint>(nullable: false),
                    country_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    type_data = table.Column<int>(nullable: false),
                    type_data2 = table.Column<int>(nullable: false),
                    appear_game_date_time = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delay_effects", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_skills");

            migrationBuilder.DropTable(
                name: "delay_effects");

            migrationBuilder.DropColumn(
                name: "skill_point",
                table: "characters");
        }
    }
}
