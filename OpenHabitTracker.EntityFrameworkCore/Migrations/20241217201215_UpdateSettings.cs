using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedCategoryId",
                table: "Settings");

            migrationBuilder.AddColumn<string>(
                name: "HiddenCategoryIds",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenCategoryIds",
                table: "Settings");

            migrationBuilder.AddColumn<long>(
                name: "SelectedCategoryId",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
