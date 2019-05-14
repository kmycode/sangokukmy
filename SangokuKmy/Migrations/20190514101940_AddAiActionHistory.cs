using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddAiActionHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attacker_soldiers_money",
                table: "ai_battle_histories",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ai_action_histories",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    game_date_time = table.Column<int>(nullable: false),
                    rice_price = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_action_histories", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_action_histories");

            migrationBuilder.DropColumn(
                name: "attacker_soldiers_money",
                table: "ai_battle_histories");
        }
    }
}
