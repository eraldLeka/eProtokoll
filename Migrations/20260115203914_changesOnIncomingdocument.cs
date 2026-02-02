using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class changesOnIncomingdocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderPhone",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SenderPosition",
                table: "Documents");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenderPhone",
                table: "Documents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderPosition",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

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
    }
}
