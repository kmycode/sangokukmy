using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCharacterCommander : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "last_all_commander_id",
                table: "char_message_read",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "last_attribute_commander_id",
                table: "char_message_read",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "last_from_commander_id",
                table: "char_message_read",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "last_private_commander_id",
                table: "char_message_read",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "country_commanders",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    subject = table.Column<short>(nullable: false),
                    subject_data = table.Column<uint>(nullable: false),
                    subject_data2 = table.Column<uint>(nullable: false),
                    message = table.Column<string>(type: "varchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_commanders", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_commanders");

            migrationBuilder.DropColumn(
                name: "last_all_commander_id",
                table: "char_message_read");

            migrationBuilder.DropColumn(
                name: "last_attribute_commander_id",
                table: "char_message_read");

            migrationBuilder.DropColumn(
                name: "last_from_commander_id",
                table: "char_message_read");

            migrationBuilder.DropColumn(
                name: "last_private_commander_id",
                table: "char_message_read");
        }
    }
}
