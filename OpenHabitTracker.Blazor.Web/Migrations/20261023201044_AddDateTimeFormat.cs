using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.Blazor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDateTimeFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DateFormat",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeFormat",
                table: "Settings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateFormat",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TimeFormat",
                table: "Settings");
        }
    }
}
