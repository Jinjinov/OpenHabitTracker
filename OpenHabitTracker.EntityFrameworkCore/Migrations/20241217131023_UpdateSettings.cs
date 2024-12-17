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
            migrationBuilder.AddColumn<int>(
                name: "CategoryFilterLogic",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriorityFilterLogic",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SelectedCategoryIds",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "SelectedPriority",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryFilterLogic",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PriorityFilterLogic",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SelectedCategoryIds",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SelectedPriority",
                table: "Settings");
        }
    }
}
