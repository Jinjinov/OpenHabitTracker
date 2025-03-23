using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddFilterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryFilterDisplay",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FoldSection",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HideCompletedTasks",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PriorityFilterDisplay",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SelectedCategoryId",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedPriority",
                table: "Settings",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryFilterDisplay",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "FoldSection",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "HideCompletedTasks",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PriorityFilterDisplay",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SelectedCategoryId",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SelectedPriority",
                table: "Settings");
        }
    }
}
