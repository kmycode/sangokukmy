using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddAiCountryParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "seiran_policy",
                table: "ai_country_managements",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "ai_battle_histories",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    country_id = table.Column<uint>(nullable: false),
                    town_id = table.Column<uint>(nullable: false),
                    town_country_id = table.Column<uint>(nullable: false),
                    game_date_time = table.Column<int>(nullable: false),
                    target_type = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_battle_histories", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_battle_histories");

            migrationBuilder.DropColumn(
                name: "seiran_policy",
                table: "ai_country_managements");
        }
    }
}
