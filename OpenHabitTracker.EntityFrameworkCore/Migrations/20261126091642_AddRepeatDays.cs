using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRepeatDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepeatMonthDays",
                table: "Contents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatMonthWeeks",
                table: "Contents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatMonths",
                table: "Contents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatWeekdays",
                table: "Contents",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepeatMonthDays",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "RepeatMonthWeeks",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "RepeatMonths",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "RepeatWeekdays",
                table: "Contents");
        }
    }
}
