using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class ConvertClassificationToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Classifications_ClassificationId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "Classifications");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ClassificationId",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "ClassificationId",
                table: "Documents",
                newName: "Classification");

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 11, 12, 51, 49, 867, DateTimeKind.Local).AddTicks(2313));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Classification",
                table: "Documents",
                newName: "ClassificationId");

            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    ClassificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RetentionYears = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.ClassificationId);
                });

            migrationBuilder.InsertData(
                table: "Classifications",
                columns: new[] { "ClassificationId", "ColorCode", "CreatedBy", "CreatedDate", "Description", "IsActive", "IsDefault", "Level", "ModifiedBy", "ModifiedDate", "Name", "RetentionYears", "SortOrder" },
                values: new object[,]
                {
                    { 1, "#28a745", null, new DateTime(2026, 2, 17, 15, 59, 6, 558, DateTimeKind.Local).AddTicks(9934), "Dokumente publike që mund të shihen nga të gjithë", true, true, 0, null, null, "Publik", 5, 1 },
                    { 2, "#ffc107", null, new DateTime(2026, 2, 17, 15, 59, 6, 558, DateTimeKind.Local).AddTicks(9944), "Vetëm për punonjësit e përzgjedhur (assigned)", true, false, 1, null, null, "I Kufizuar", 10, 2 },
                    { 3, "#dc3545", null, new DateTime(2026, 2, 17, 15, 59, 6, 558, DateTimeKind.Local).AddTicks(9952), "Vetëm menaxherët dhe administratorët", true, false, 2, null, null, "Sekret", 20, 3 }
                });

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 2, 17, 15, 59, 6, 559, DateTimeKind.Local).AddTicks(386));

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClassificationId",
                table: "Documents",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_Level",
                table: "Classifications",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_Name",
                table: "Classifications",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Classifications_ClassificationId",
                table: "Documents",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
