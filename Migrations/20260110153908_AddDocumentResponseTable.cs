using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentResponseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentResponses",
                columns: table => new
                {
                    ResponseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingId = table.Column<int>(type: "int", nullable: false),
                    ResponseSubject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResponseNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ScannedPdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ScannedPdfName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ScannedPdfSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OutgoingDocumentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentResponses", x => x.ResponseId);
                    table.ForeignKey(
                        name: "FK_DocumentResponses_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentResponses_DocumentTrackings_TrackingId",
                        column: x => x.TrackingId,
                        principalTable: "DocumentTrackings",
                        principalColumn: "TrackingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentResponses_Documents_OutgoingDocumentId",
                        column: x => x.OutgoingDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                });

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 10, 16, 39, 5, 205, DateTimeKind.Local).AddTicks(1326));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 10, 16, 39, 5, 205, DateTimeKind.Local).AddTicks(1343));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 10, 16, 39, 5, 205, DateTimeKind.Local).AddTicks(1356));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 10, 16, 39, 5, 205, DateTimeKind.Local).AddTicks(1773));

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResponses_ApprovedByUserId",
                table: "DocumentResponses",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResponses_OutgoingDocumentId",
                table: "DocumentResponses",
                column: "OutgoingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResponses_TrackingId",
                table: "DocumentResponses",
                column: "TrackingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentResponses");

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 7, 16, 11, 55, 780, DateTimeKind.Local).AddTicks(2019));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 7, 16, 11, 55, 780, DateTimeKind.Local).AddTicks(2058));

            migrationBuilder.UpdateData(
                table: "Classifications",
                keyColumn: "ClassificationId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 7, 16, 11, 55, 780, DateTimeKind.Local).AddTicks(2070));

            migrationBuilder.UpdateData(
                table: "ProtocolSettings",
                keyColumn: "ProtocolSettingsId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 1, 7, 16, 11, 55, 780, DateTimeKind.Local).AddTicks(2657));
        }
    }
}
