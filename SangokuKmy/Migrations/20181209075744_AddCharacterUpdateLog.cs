using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCharacterUpdateLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "alias_id",
                table: "characters",
                type: "varchar(32)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterUpdateLogs",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    is_first_at_month = table.Column<bool>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    date = table.Column<DateTime>(nullable: false),
                    game_date = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterUpdateLogs", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterUpdateLogs");

            migrationBuilder.DropColumn(
                name: "alias_id",
                table: "characters");
        }
    }
}
