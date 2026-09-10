using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalSettings : Migration
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

            migrationBuilder.AddColumn<bool>(
                name: "NotificationIncludeOverdueTasks",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<TimeOnly>(
                name: "NotificationTime",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHabitCharts",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
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
                name: "NotificationIncludeOverdueTasks",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationLeadMinutes",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationMinimumPriority",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NotificationTime",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ShowHabitCharts",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Settings");
        }
    }
}
