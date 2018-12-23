using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCharacterCommandParameter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "character_commands",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "character_command_parameters",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_command_id = table.Column<uint>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    number_value = table.Column<int>(nullable: true),
                    string_value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_command_parameters", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_command_parameters");

            migrationBuilder.DropColumn(
                name: "name",
                table: "character_commands");
        }
    }
}
