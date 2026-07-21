using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanReferenceAndExerciseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "WorkoutExercises",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Sets",
                type: "TEXT",
                precision: 10,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Sets");
        }
    }
}
