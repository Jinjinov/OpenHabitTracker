using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.Blazor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Times",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Contents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            // Every existing row belonged to the single account, which startup creates before anything
            // else can, so it has the lowest id. Categories and settings are included because their
            // UserId was written by clients and may hold a device's local id. Rows left at zero are never
            // shown once queries filter by owner.
            foreach (string table in new[] { "Contents", "Items", "Times", "Categories", "Settings" })
            {
                migrationBuilder.Sql($"UPDATE \"{table}\" SET \"UserId\" = (SELECT MIN(\"Id\") FROM \"AspNetUsers\") WHERE EXISTS (SELECT 1 FROM \"AspNetUsers\");");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Times_UserId_HabitId",
                table: "Times",
                columns: new[] { "UserId", "HabitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UserId",
                table: "Settings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_ParentId",
                table: "Items",
                columns: new[] { "UserId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Contents_UserId",
                table: "Contents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId",
                table: "Categories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Times_UserId_HabitId",
                table: "Times");

            migrationBuilder.DropIndex(
                name: "IX_Settings_UserId",
                table: "Settings");

            migrationBuilder.DropIndex(
                name: "IX_Items_UserId_ParentId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Contents_UserId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Categories_UserId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Times");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Contents");
        }
    }
}
