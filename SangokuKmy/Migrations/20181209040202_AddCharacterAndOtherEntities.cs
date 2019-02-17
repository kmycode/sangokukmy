using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCharacterAndOtherEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "char_messages",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    character_icon_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    type_data = table.Column<uint>(nullable: false),
                    type_data_2 = table.Column<uint>(nullable: false),
                    message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_commands",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    game_date = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_commands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_icons",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    is_main = table.Column<bool>(nullable: false),
                    type = table.Column<byte>(nullable: false),
                    uri = table.Column<string>(nullable: true),
                    file_name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_icons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_logs",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    message = table.Column<string>(nullable: true),
                    date = table.Column<DateTime>(nullable: false),
                    game_date = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    password_hash = table.Column<string>(type: "varchar(256)", nullable: true),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    country_id = table.Column<uint>(nullable: false),
                    strong = table.Column<short>(nullable: false),
                    strong_ex = table.Column<short>(nullable: false),
                    intellect = table.Column<short>(nullable: false),
                    intellect_ex = table.Column<short>(nullable: false),
                    leadership = table.Column<short>(nullable: false),
                    leadership_ex = table.Column<short>(nullable: false),
                    popularity = table.Column<short>(nullable: false),
                    popularity_ex = table.Column<short>(nullable: false),
                    soldier_number = table.Column<int>(nullable: false),
                    proficiency = table.Column<short>(nullable: false),
                    money = table.Column<int>(nullable: false),
                    rice = table.Column<int>(nullable: false),
                    contribution = table.Column<int>(nullable: false),
                    @class = table.Column<int>(name: "class", nullable: false),
                    delete_turn = table.Column<short>(nullable: false),
                    town_id = table.Column<uint>(nullable: false),
                    message = table.Column<string>(nullable: true),
                    last_updated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    country_color_id = table.Column<short>(nullable: false),
                    established = table.Column<int>(nullable: false),
                    capital_town_id = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_messages",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    country_id = table.Column<uint>(nullable: false),
                    type = table.Column<byte>(nullable: false),
                    message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_posts",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    country_id = table.Column<uint>(nullable: false),
                    type = table.Column<short>(nullable: false),
                    character_id = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "m_default_icons",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    file_name = table.Column<string>(type: "varchar(256)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_m_default_icons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "map_logs",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    is_important = table.Column<bool>(nullable: false),
                    event_name = table.Column<string>(type: "varchar(64)", nullable: true),
                    event_color = table.Column<string>(type: "varchar(12)", nullable: true),
                    message = table.Column<string>(nullable: true),
                    game_date = table.Column<int>(nullable: false),
                    date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_data",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    period = table.Column<short>(nullable: false),
                    beta_version = table.Column<short>(nullable: false),
                    game_date_time = table.Column<int>(nullable: false),
                    current_month_start_date_time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "town",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<byte>(nullable: false),
                    country_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    x = table.Column<short>(nullable: false),
                    y = table.Column<short>(nullable: false),
                    people = table.Column<int>(nullable: false),
                    agriculture = table.Column<int>(nullable: false),
                    agriculture_max = table.Column<int>(nullable: false),
                    commercial = table.Column<int>(nullable: false),
                    commercial_max = table.Column<int>(nullable: false),
                    technology = table.Column<int>(nullable: false),
                    technology_max = table.Column<int>(nullable: false),
                    wall = table.Column<int>(nullable: false),
                    wall_max = table.Column<int>(nullable: false),
                    wallguard = table.Column<int>(nullable: false),
                    wallguard_max = table.Column<int>(nullable: false),
                    security = table.Column<short>(nullable: false),
                    rice_price = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_town", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "town_defenders",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    town_id = table.Column<uint>(nullable: false),
                    character_id = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_town_defenders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unit_members",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    character_id = table.Column<uint>(nullable: false),
                    unit_id = table.Column<uint>(nullable: false),
                    post = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    country_id = table.Column<uint>(nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: true),
                    message = table.Column<string>(nullable: true),
                    is_limited = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "m_default_icons",
                columns: new[] { "id", "file_name" },
                values: new object[,]
                {
                    { 1u, "0.gif" },
                    { 72u, "71.gif" },
                    { 71u, "70.gif" },
                    { 70u, "69.gif" },
                    { 69u, "68.gif" },
                    { 68u, "67.gif" },
                    { 67u, "66.gif" },
                    { 66u, "65.gif" },
                    { 65u, "64.gif" },
                    { 64u, "63.gif" },
                    { 63u, "62.gif" },
                    { 62u, "61.gif" },
                    { 61u, "60.gif" },
                    { 60u, "59.gif" },
                    { 59u, "58.gif" },
                    { 58u, "57.gif" },
                    { 57u, "56.gif" },
                    { 56u, "55.gif" },
                    { 55u, "54.gif" },
                    { 54u, "53.gif" },
                    { 53u, "52.gif" },
                    { 52u, "51.gif" },
                    { 73u, "72.gif" },
                    { 51u, "50.gif" },
                    { 74u, "73.gif" },
                    { 76u, "75.gif" },
                    { 97u, "96.gif" },
                    { 96u, "95.gif" },
                    { 95u, "94.gif" },
                    { 94u, "93.gif" },
                    { 93u, "92.gif" },
                    { 92u, "91.gif" },
                    { 91u, "90.gif" },
                    { 90u, "89.gif" },
                    { 89u, "88.gif" },
                    { 88u, "87.gif" },
                    { 87u, "86.gif" },
                    { 86u, "85.gif" },
                    { 85u, "84.gif" },
                    { 84u, "83.gif" },
                    { 83u, "82.gif" },
                    { 82u, "81.gif" },
                    { 81u, "80.gif" },
                    { 80u, "79.gif" },
                    { 79u, "78.gif" },
                    { 78u, "77.gif" },
                    { 77u, "76.gif" },
                    { 75u, "74.gif" },
                    { 98u, "97.gif" },
                    { 50u, "49.gif" },
                    { 48u, "47.gif" },
                    { 22u, "21.gif" },
                    { 21u, "20.gif" },
                    { 20u, "19.gif" },
                    { 19u, "18.gif" },
                    { 18u, "17.gif" },
                    { 17u, "16.gif" },
                    { 16u, "15.gif" },
                    { 15u, "14.gif" },
                    { 14u, "13.gif" },
                    { 13u, "12.gif" },
                    { 12u, "11.gif" },
                    { 11u, "10.gif" },
                    { 10u, "9.gif" },
                    { 9u, "8.gif" },
                    { 8u, "7.gif" },
                    { 7u, "6.gif" },
                    { 6u, "5.gif" },
                    { 5u, "4.gif" },
                    { 4u, "3.gif" },
                    { 3u, "2.gif" },
                    { 2u, "1.gif" },
                    { 23u, "22.gif" },
                    { 49u, "48.gif" },
                    { 24u, "23.gif" },
                    { 26u, "25.gif" },
                    { 47u, "46.gif" },
                    { 46u, "45.gif" },
                    { 45u, "44.gif" },
                    { 44u, "43.gif" },
                    { 43u, "42.gif" },
                    { 42u, "41.gif" },
                    { 41u, "40.gif" },
                    { 40u, "39.gif" },
                    { 39u, "38.gif" },
                    { 38u, "37.gif" },
                    { 37u, "36.gif" },
                    { 36u, "35.gif" },
                    { 35u, "34.gif" },
                    { 34u, "33.gif" },
                    { 33u, "32.gif" },
                    { 32u, "31.gif" },
                    { 31u, "30.gif" },
                    { 30u, "29.gif" },
                    { 29u, "28.gif" },
                    { 28u, "27.gif" },
                    { 27u, "26.gif" },
                    { 25u, "24.gif" },
                    { 99u, "98.gif" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "char_messages");

            migrationBuilder.DropTable(
                name: "character_commands");

            migrationBuilder.DropTable(
                name: "character_icons");

            migrationBuilder.DropTable(
                name: "character_logs");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "country_messages");

            migrationBuilder.DropTable(
                name: "country_posts");

            migrationBuilder.DropTable(
                name: "m_default_icons");

            migrationBuilder.DropTable(
                name: "map_logs");

            migrationBuilder.DropTable(
                name: "system_data");

            migrationBuilder.DropTable(
                name: "town");

            migrationBuilder.DropTable(
                name: "town_defenders");

            migrationBuilder.DropTable(
                name: "unit_members");

            migrationBuilder.DropTable(
                name: "units");
        }
    }
}
