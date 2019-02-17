using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryIncomes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_color",
                table: "map_logs");

            migrationBuilder.DropColumn(
                name: "event_name",
                table: "map_logs");

            migrationBuilder.AddColumn<short>(
                name: "event_type",
                table: "map_logs",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "last_money_incomes",
                table: "countries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "last_rice_incomes",
                table: "countries",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "event_type",
                table: "map_logs");

            migrationBuilder.DropColumn(
                name: "last_money_incomes",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "last_rice_incomes",
                table: "countries");

            migrationBuilder.AddColumn<string>(
                name: "event_color",
                table: "map_logs",
                type: "varchar(12)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_name",
                table: "map_logs",
                type: "varchar(64)",
                nullable: true);
        }
    }
}
