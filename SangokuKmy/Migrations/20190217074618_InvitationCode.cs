using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class InvitationCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "invitation_code_requested_entry",
                table: "system_data",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "invitation_code",
                columns: table => new
                {
                    id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    aim = table.Column<short>(nullable: false),
                    code = table.Column<string>(type: "varchar(64)", nullable: true),
                    has_used = table.Column<bool>(nullable: false),
                    character_id = table.Column<uint>(nullable: false),
                    used = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitation_code", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitation_code");

            migrationBuilder.DropColumn(
                name: "invitation_code_requested_entry",
                table: "system_data");
        }
    }
}
