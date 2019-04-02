using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddHistoryMapLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historical_maplogs",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    history_id = table.Column<uint>(nullable: false),
                    event_type = table.Column<short>(nullable: false),
                    message = table.Column<string>(nullable: true),
                    game_date = table.Column<int>(nullable: false),
                    date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_maplogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historical_towns",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    history_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    x = table.Column<short>(nullable: false),
                    y = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_towns", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_maplogs");

            migrationBuilder.DropTable(
                name: "historical_towns");
        }
    }
}
