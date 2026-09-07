using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddShowHabitCharts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowHabitCharts",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowHabitCharts",
                table: "Settings");
        }
    }
}
