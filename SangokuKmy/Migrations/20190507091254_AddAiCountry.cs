using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddAiCountry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "management_count",
                table: "system_data",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "ai_country_managements",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    country_id = table.Column<uint>(nullable: false),
                    war_policy = table.Column<short>(nullable: false),
                    policy_target = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_country_managements", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_country_managements");

            migrationBuilder.DropColumn(
                name: "management_count",
                table: "system_data");
        }
    }
}
