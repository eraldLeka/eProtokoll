using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class Globalnumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentNumber_Year_DocumentType",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentNumber_Year",
                table: "Documents",
                columns: new[] { "DocumentNumber", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentNumber_Year",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentNumber_Year_DocumentType",
                table: "Documents",
                columns: new[] { "DocumentNumber", "Year", "DocumentType" },
                unique: true);
        }
    }
}
