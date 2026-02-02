using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class changesOnOutgoingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutgoingDocument_CarbonCopyList",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RecipientAddress",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RecipientPosition",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignedDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignerPosition",
                table: "Documents");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutgoingDocument_CarbonCopyList",
                table: "Documents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientAddress",
                table: "Documents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "Documents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPosition",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedBy",
                table: "Documents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignerPosition",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 11, 57, 37, 756, DateTimeKind.Local).AddTicks(9925));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 11, 57, 37, 756, DateTimeKind.Local).AddTicks(9932));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 11, 57, 37, 756, DateTimeKind.Local).AddTicks(9937));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 15, 11, 57, 37, 757, DateTimeKind.Local).AddTicks(417));
        }
    }
}
