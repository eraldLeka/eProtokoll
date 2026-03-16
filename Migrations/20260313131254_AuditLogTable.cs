using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 13, 14, 12, 53, 325, DateTimeKind.Local).AddTicks(1712));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 12, 15, 18, 50, 71, DateTimeKind.Local).AddTicks(6491));
        }
    }
}
