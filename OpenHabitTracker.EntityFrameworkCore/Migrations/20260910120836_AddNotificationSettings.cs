using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationContent",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationHabitThreshold",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "NotificationHour",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationIncludeOverdueTasks",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "NotificationLeadMinutes",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotificationMinimumPriority",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationContent",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationHabitThreshold",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationHour",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationIncludeOverdueTasks",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationLeadMinutes",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationMinimumPriority",
                table: "Settings");
        }
    }
}
