using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class EntryData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uri",
                table: "character_icons");

            migrationBuilder.InsertData(
                table: "initial_town",
                columns: new[] { "id", "name", "type", "x", "y" },
                values: new object[,]
                {
                    { 1u, "長安", (byte)4, (short)4, (short)4 },
                    { 2u, "成都", (byte)0, (short)3, (short)5 },
                    { 3u, "鄴", (byte)0, (short)5, (short)4 },
                    { 4u, "襄陽", (byte)0, (short)4, (short)5 },
                    { 5u, "建業", (byte)0, (short)5, (short)5 }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "initial_town",
                keyColumn: "id",
                keyValue: 1u);

            migrationBuilder.DeleteData(
                table: "initial_town",
                keyColumn: "id",
                keyValue: 2u);

            migrationBuilder.DeleteData(
                table: "initial_town",
                keyColumn: "id",
                keyValue: 3u);

            migrationBuilder.DeleteData(
                table: "initial_town",
                keyColumn: "id",
                keyValue: 4u);

            migrationBuilder.DeleteData(
                table: "initial_town",
                keyColumn: "id",
                keyValue: 5u);

            migrationBuilder.AddColumn<string>(
                name: "uri",
                table: "character_icons",
                nullable: true);
        }
    }
}
