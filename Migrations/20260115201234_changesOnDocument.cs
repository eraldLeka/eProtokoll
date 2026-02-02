using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class changesOnDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsScanned",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PageCount",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReferenceDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Documents");

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 12, 33, 608, DateTimeKind.Local).AddTicks(5253));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 12, 33, 608, DateTimeKind.Local).AddTicks(5262));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 12, 33, 608, DateTimeKind.Local).AddTicks(5270));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 21, 12, 33, 608, DateTimeKind.Local).AddTicks(5769));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScanned",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PageCount",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferenceDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 20, 27, 8, 395, DateTimeKind.Local).AddTicks(8932));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 20, 27, 8, 395, DateTimeKind.Local).AddTicks(8941));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 20, 27, 8, 395, DateTimeKind.Local).AddTicks(8949));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 20, 27, 8, 395, DateTimeKind.Local).AddTicks(9398));
        }
    }
}
