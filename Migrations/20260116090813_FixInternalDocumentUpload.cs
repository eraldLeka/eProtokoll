using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class FixInternalDocumentUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarbonCopyList",
                table: "Documents");

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 16, 10, 8, 12, 716, DateTimeKind.Local).AddTicks(2732));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 16, 10, 8, 12, 716, DateTimeKind.Local).AddTicks(2736));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 16, 10, 8, 12, 716, DateTimeKind.Local).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 16, 10, 8, 12, 716, DateTimeKind.Local).AddTicks(3054));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarbonCopyList",
                table: "Documents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 39, 13, 25, DateTimeKind.Local).AddTicks(8149));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 39, 13, 25, DateTimeKind.Local).AddTicks(8158));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 39, 13, 25, DateTimeKind.Local).AddTicks(8167));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 39, 13, 25, DateTimeKind.Local).AddTicks(8633));
        }
    }
}
