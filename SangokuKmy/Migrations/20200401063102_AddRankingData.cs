using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddRankingData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "battle_broke_wall_size",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_continuous_count",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_dominate_count",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_lost_count",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_won_count",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "class",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "money",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "rice",
                table: "historical_characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_broke_wall_size",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_continuous_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_dominate_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_lost_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "battle_won_count",
                table: "characters",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "battle_broke_wall_size",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "battle_continuous_count",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "battle_dominate_count",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "battle_lost_count",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "battle_won_count",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "class",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "money",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "rice",
                table: "historical_characters");

            migrationBuilder.DropColumn(
                name: "battle_broke_wall_size",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_continuous_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_dominate_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_lost_count",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "battle_won_count",
                table: "characters");
        }
    }
}
