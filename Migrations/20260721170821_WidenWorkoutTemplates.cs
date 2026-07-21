using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTracker.Migrations
{
    /// <inheritdoc />
    public partial class WidenWorkoutTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WorkoutTemplates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "CatalogKey",
                table: "WorkoutTemplates",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultSets",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "DefaultReps",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultDistance",
                table: "WorkoutTemplateExercises",
                type: "TEXT",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultDurationSeconds",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplates_CatalogKey",
                table: "WorkoutTemplates",
                column: "CatalogKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutTemplates_CatalogKey",
                table: "WorkoutTemplates");

            migrationBuilder.DropColumn(
                name: "CatalogKey",
                table: "WorkoutTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultDistance",
                table: "WorkoutTemplateExercises");

            migrationBuilder.DropColumn(
                name: "DefaultDurationSeconds",
                table: "WorkoutTemplateExercises");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WorkoutTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultSets",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultReps",
                table: "WorkoutTemplateExercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
