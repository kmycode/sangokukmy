﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class AddCountryAllianceChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_breaking_delay",
                table: "country_alliances");

            migrationBuilder.AddColumn<uint>(
                name: "change_target_id",
                table: "country_alliances",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "change_target_id",
                table: "country_alliances");

            migrationBuilder.AddColumn<int>(
                name: "new_breaking_delay",
                table: "country_alliances",
                nullable: false,
                defaultValue: 0);
        }
    }
}
