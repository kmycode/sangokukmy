using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class DebugUpdatableLastDateTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updatable_game_date_time",
                table: "system_debug");

            migrationBuilder.AddColumn<DateTime>(
                name: "updatable_last_date",
                table: "system_debug",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updatable_last_date",
                table: "system_debug");

            migrationBuilder.AddColumn<int>(
                name: "updatable_game_date_time",
                table: "system_debug",
                nullable: false,
                defaultValue: 0);
        }
    }
}
