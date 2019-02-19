using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historical_character_icons",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    type = table.Column<byte>(nullable: false),
                    file_name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_character_icons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historical_characters",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    history_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    country_id = table.Column<uint>(nullable: false),
                    post_type = table.Column<short>(nullable: false),
                    strong = table.Column<short>(nullable: false),
                    intellect = table.Column<short>(nullable: false),
                    leadership = table.Column<short>(nullable: false),
                    popularity = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_characters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "historical_countries",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    history_id = table.Column<uint>(nullable: false),
                    country_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    country_color_id = table.Column<short>(nullable: false),
                    established = table.Column<int>(nullable: false),
                    has_overthrown = table.Column<bool>(nullable: false),
                    overthrown_game_date = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historical_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "histories",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    period = table.Column<short>(nullable: false),
                    beta_version = table.Column<short>(nullable: false),
                    unified_date_time = table.Column<DateTime>(nullable: false),
                    unified_country_message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_histories", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_character_icons");

            migrationBuilder.DropTable(
                name: "historical_characters");

            migrationBuilder.DropTable(
                name: "historical_countries");

            migrationBuilder.DropTable(
                name: "histories");
        }
    }
}
