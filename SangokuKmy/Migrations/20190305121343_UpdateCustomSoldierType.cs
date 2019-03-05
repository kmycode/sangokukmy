using Microsoft.EntityFrameworkCore.Migrations;

namespace SangokuKmy.Migrations
{
    public partial class UpdateCustomSoldierType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_attack",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "base_defend",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "continuous_attack",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "continuous_defend",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "continuous_probability",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "intellect_attack",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "intellect_defend",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "is_conscript_disabled",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "leadership_attack",
                table: "character_soldier_types");

            migrationBuilder.DropColumn(
                name: "leadership_defend",
                table: "character_soldier_types");

            migrationBuilder.RenameColumn(
                name: "wall_defend",
                table: "character_soldier_types",
                newName: "strong_guards");

            migrationBuilder.RenameColumn(
                name: "wall_attack",
                table: "character_soldier_types",
                newName: "strong_crossbow");

            migrationBuilder.RenameColumn(
                name: "through_defenders_probability",
                table: "character_soldier_types",
                newName: "seiran");

            migrationBuilder.RenameColumn(
                name: "technology",
                table: "character_soldier_types",
                newName: "repeating_crossbow");

            migrationBuilder.RenameColumn(
                name: "strong_defend",
                table: "character_soldier_types",
                newName: "light_intellect");

            migrationBuilder.RenameColumn(
                name: "strong_attack",
                table: "character_soldier_types",
                newName: "light_infantory");

            migrationBuilder.RenameColumn(
                name: "rush_probability",
                table: "character_soldier_types",
                newName: "light_cavalry");

            migrationBuilder.RenameColumn(
                name: "rush_defend",
                table: "character_soldier_types",
                newName: "intellect");

            migrationBuilder.RenameColumn(
                name: "rush_attack",
                table: "character_soldier_types",
                newName: "heavy_infantory");

            migrationBuilder.RenameColumn(
                name: "rush_against_defend",
                table: "character_soldier_types",
                newName: "heavy_cavalry");

            migrationBuilder.RenameColumn(
                name: "rush_against_attack",
                table: "character_soldier_types",
                newName: "guard_4");

            migrationBuilder.RenameColumn(
                name: "recovery",
                table: "character_soldier_types",
                newName: "guard_3");

            migrationBuilder.RenameColumn(
                name: "preset",
                table: "character_soldier_types",
                newName: "guard_2");

            migrationBuilder.RenameColumn(
                name: "popularity_defend",
                table: "character_soldier_types",
                newName: "guard_1");

            migrationBuilder.RenameColumn(
                name: "popularity_attack",
                table: "character_soldier_types",
                newName: "common_soldier");

            migrationBuilder.RenameColumn(
                name: "money",
                table: "character_soldier_types",
                newName: "archer");

            migrationBuilder.AddColumn<uint>(
                name: "character_soldier_type_id",
                table: "characters",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "character_soldier_type_id",
                table: "characters");

            migrationBuilder.RenameColumn(
                name: "strong_guards",
                table: "character_soldier_types",
                newName: "wall_defend");

            migrationBuilder.RenameColumn(
                name: "strong_crossbow",
                table: "character_soldier_types",
                newName: "wall_attack");

            migrationBuilder.RenameColumn(
                name: "seiran",
                table: "character_soldier_types",
                newName: "through_defenders_probability");

            migrationBuilder.RenameColumn(
                name: "repeating_crossbow",
                table: "character_soldier_types",
                newName: "technology");

            migrationBuilder.RenameColumn(
                name: "light_intellect",
                table: "character_soldier_types",
                newName: "strong_defend");

            migrationBuilder.RenameColumn(
                name: "light_infantory",
                table: "character_soldier_types",
                newName: "strong_attack");

            migrationBuilder.RenameColumn(
                name: "light_cavalry",
                table: "character_soldier_types",
                newName: "rush_probability");

            migrationBuilder.RenameColumn(
                name: "intellect",
                table: "character_soldier_types",
                newName: "rush_defend");

            migrationBuilder.RenameColumn(
                name: "heavy_infantory",
                table: "character_soldier_types",
                newName: "rush_attack");

            migrationBuilder.RenameColumn(
                name: "heavy_cavalry",
                table: "character_soldier_types",
                newName: "rush_against_defend");

            migrationBuilder.RenameColumn(
                name: "guard_4",
                table: "character_soldier_types",
                newName: "rush_against_attack");

            migrationBuilder.RenameColumn(
                name: "guard_3",
                table: "character_soldier_types",
                newName: "recovery");

            migrationBuilder.RenameColumn(
                name: "guard_2",
                table: "character_soldier_types",
                newName: "preset");

            migrationBuilder.RenameColumn(
                name: "guard_1",
                table: "character_soldier_types",
                newName: "popularity_defend");

            migrationBuilder.RenameColumn(
                name: "common_soldier",
                table: "character_soldier_types",
                newName: "popularity_attack");

            migrationBuilder.RenameColumn(
                name: "archer",
                table: "character_soldier_types",
                newName: "money");

            migrationBuilder.AddColumn<short>(
                name: "base_attack",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "base_defend",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "continuous_attack",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "continuous_defend",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "continuous_probability",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "intellect_attack",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "intellect_defend",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "is_conscript_disabled",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "leadership_attack",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "leadership_defend",
                table: "character_soldier_types",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
