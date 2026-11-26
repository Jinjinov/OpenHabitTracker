using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitPlannedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "PlannedTime",
                table: "Contents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedTime",
                table: "Contents");
        }
    }
}
