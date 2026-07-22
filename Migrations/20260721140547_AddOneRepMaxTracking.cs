using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddOneRepMaxTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TracksOneRepMax",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Existing databases are past the seeder's "only if empty" guard, so backfill here with
            // the same rule DbInitializer.IsWeightLoaded applies to fresh installs: strength work
            // carries an external load unless the equipment is the lifter. Deliberately spelled out
            // in SQL rather than calling the seeder — a migration has to keep meaning what it meant
            // on the day it ran, however that rule is later changed.
            migrationBuilder.Sql(
                """
                UPDATE Exercises
                SET TracksOneRepMax = 1
                WHERE Category = 'Strength'
                  AND Equipment IS NOT NULL
                  AND TRIM(Equipment) <> ''
                  AND Equipment NOT IN ('Bodyweight', 'None');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TracksOneRepMax",
                table: "Exercises");
        }
    }
}
