using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenHabitTracker.Blazor.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDateTimeFormatUserOwnershipAndDecimalQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "Times",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Times",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

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

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<double>(
                name: "TargetQuantity",
                table: "Contents",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Contents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            // The UserId of these two was written by clients and may hold a device's local id. Zero marks
            // them as unowned like the new columns, and startup gives every unowned row to the configured
            // account, which a migration cannot read.
            migrationBuilder.Sql("UPDATE \"Categories\" SET \"UserId\" = 0;");
            migrationBuilder.Sql("UPDATE \"Settings\" SET \"UserId\" = 0;");

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
                name: "DateFormat",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TimeFormat",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Contents");

            migrationBuilder.AlterColumn<long>(
                name: "Quantity",
                table: "Times",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<long>(
                name: "TargetQuantity",
                table: "Contents",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);
        }
    }
}
