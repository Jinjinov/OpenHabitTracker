using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.Blazor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRelativeDateFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoneFromDayOffset",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoneToDayOffset",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedFromDayOffset",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedToDayOffset",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDoneInRange",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoneFromDayOffset",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "DoneToDayOffset",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PlannedFromDayOffset",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PlannedToDayOffset",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ShowDoneInRange",
                table: "Settings");
        }
    }
}
