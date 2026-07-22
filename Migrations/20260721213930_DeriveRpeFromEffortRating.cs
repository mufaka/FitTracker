using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTracker.Migrations
{
    /// <inheritdoc />
    public partial class DeriveRpeFromEffortRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRpeDerived",
                table: "Sets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRpeDerived",
                table: "Sets");
        }
    }
}
