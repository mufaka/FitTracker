using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTracker.Migrations
{
    /// <summary>
    /// Raises the declared precision of every stored weight from (10, 2) to (10, 4) — Set.Weight,
    /// BodyMeasurement.Weight, PersonalRecord.Weight and PersonalRecord.OneRepMax — now that weights
    /// are persisted canonically in kilograms and converted only for display. Two decimals do not
    /// survive the round trip: 45 lbs would store as 20.41 kg and come back as 44.99 lbs.
    ///
    /// Deliberately empty, and kept rather than deleted. SQLite has no decimal type and EF maps
    /// decimal to TEXT, so precision and scale are model metadata that never reach the database:
    /// there is no column to alter. The change is still real — it is recorded in the model snapshot,
    /// it states the intent for any future provider, and the rounding it describes is enforced in
    /// code by <c>UnitConverter</c>. Removing the migration would leave the snapshot ahead of the
    /// migration history.
    ///
    /// NOTE — existing rows are NOT back-filled, and this is deliberate. Before this change a weight
    /// held whatever number the user typed in their own unit; from here it means kilograms. Any row
    /// written earlier by an lbs user is therefore reinterpreted, and will read about 2.2x too high.
    /// The application has never been deployed and has no production data, so the reset path is the
    /// documented one: delete FitTracker.db and let migrations and seeding rebuild it. A conversion
    /// UPDATE keyed on each user's PreferredUnits would be the alternative if that ever stops being
    /// true — it is not written here because it can only be run once and cannot tell an already-
    /// converted database from an unconverted one.
    /// </summary>
    public partial class CanonicalMeasurementPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
