using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddFormation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "formation_point",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "formation_type",
                table: "characters",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "formation_type",
                table: "character_caches",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "formations",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formations", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "formations");

            migrationBuilder.DropColumn(
                name: "formation_point",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "formation_type",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "formation_type",
                table: "character_caches");
        }
    }
}
