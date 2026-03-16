using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    public partial class newDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_ProtocolDate",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ProtocolNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProtocolDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProtocolNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProtocolTime",
                table: "Documents");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentNumber_Year_DocumentType",
                table: "Documents",
                columns: new[] { "DocumentNumber", "Year", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Year",
                table: "Documents",
                column: "Year");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DocumentNumber_Year_DocumentType",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_Year",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Documents");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Documents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProtocolDate",
                table: "Documents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ProtocolNumber",
                table: "Documents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProtocolTime",
                table: "Documents",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "ProtocolSettings",
                columns: table => new
                {
                    ProtocolSettingsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllowManualEdit = table.Column<bool>(type: "bit", nullable: false),
                    AutoResetYearly = table.Column<bool>(type: "bit", nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FiscalYearEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalYearStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IncomingCurrentNumber = table.Column<int>(type: "int", nullable: false),
                    IncomingEndNumber = table.Column<int>(type: "int", nullable: true),
                    IncomingPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IncomingStartNumber = table.Column<int>(type: "int", nullable: false),
                    IncomingSuffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InstitutionAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstitutionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstitutionEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstitutionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InstitutionPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InstitutionWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InternalCurrentNumber = table.Column<int>(type: "int", nullable: false),
                    InternalEndNumber = table.Column<int>(type: "int", nullable: true),
                    InternalPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InternalStartNumber = table.Column<int>(type: "int", nullable: false),
                    InternalSuffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumberPadding = table.Column<int>(type: "int", nullable: false),
                    OutgoingCurrentNumber = table.Column<int>(type: "int", nullable: false),
                    OutgoingEndNumber = table.Column<int>(type: "int", nullable: true),
                    OutgoingPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OutgoingStartNumber = table.Column<int>(type: "int", nullable: false),
                    OutgoingSuffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProtocolNumberFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowYearInNumber = table.Column<bool>(type: "bit", nullable: false),
                    UseSeparatorSlash = table.Column<bool>(type: "bit", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolSettings", x => x.ProtocolSettingsId);
                });

            migrationBuilder.InsertData(
                table: "ProtocolSettings",
                columns: new[] { "ProtocolSettingsId", "AllowManualEdit", "AutoResetYearly", "ClosedBy", "ClosedDate", "CreatedBy", "CreatedDate", "FiscalYearEnd", "FiscalYearStart", "IncomingCurrentNumber", "IncomingEndNumber", "IncomingPrefix", "IncomingStartNumber", "IncomingSuffix", "InstitutionAddress", "InstitutionCode", "InstitutionEmail", "InstitutionName", "InstitutionPhone", "InstitutionWebsite", "InternalCurrentNumber", "InternalEndNumber", "InternalPrefix", "InternalStartNumber", "InternalSuffix", "IsActive", "IsClosed", "ModifiedBy", "ModifiedDate", "Notes", "NumberPadding", "OutgoingCurrentNumber", "OutgoingEndNumber", "OutgoingPrefix", "OutgoingStartNumber", "OutgoingSuffix", "ProtocolNumberFormat", "ShowYearInNumber", "UseSeparatorSlash", "Year" },
                values: new object[] { 1, false, true, null, null, null, new DateTime(2026, 3, 13, 14, 31, 53, 43, DateTimeKind.Local).AddTicks(7587), null, null, 1, null, "H", 1, null, null, null, null, null, null, null, 1, null, "B", 1, null, true, false, null, null, null, 4, 1, null, "D", 1, null, "{PREFIX}-{NUMBER}/{YEAR}", true, true, 2026 });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProtocolDate",
                table: "Documents",
                column: "ProtocolDate");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProtocolNumber",
                table: "Documents",
                column: "ProtocolNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolSettings_Year",
                table: "ProtocolSettings",
                column: "Year",
                unique: true);
        }
    }
}