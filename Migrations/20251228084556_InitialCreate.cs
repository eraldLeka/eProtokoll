using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eProtokoll.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedData = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    ClassificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetentionYears = table.Column<int>(type: "int", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    MinimumRoleRequired = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AllowPrint = table.Column<bool>(type: "bit", nullable: false),
                    AllowDownload = table.Column<bool>(type: "bit", nullable: false),
                    AllowCopy = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLog = table.Column<bool>(type: "bit", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.ClassificationId);
                });

            migrationBuilder.CreateTable(
                name: "Institutions",
                columns: table => new
                {
                    InstitutionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Adress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.InstitutionId);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolSettings",
                columns: table => new
                {
                    ProtocolSettingsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    IncomingStartNumber = table.Column<int>(type: "int", nullable: false),
                    IncomingCurrentNumber = table.Column<int>(type: "int", nullable: false),
                    IncomingEndNumber = table.Column<int>(type: "int", nullable: true),
                    IncomingPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IncomingSuffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OutgoingStartNumber = table.Column<int>(type: "int", nullable: false),
                    OutgoingCurrentNumber = table.Column<int>(type: "int", nullable: false),
                    OutgoingEndNumber = table.Column<int>(type: "int", nullable: true),
                    OutgoingPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OutgoingSuffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InternalStartNumber = table.Column<int>(type: "int", nullable: false),
                    InternalCurrentNumber = table.Column<int>(type: "int", nullable: false),
                    InternalEndNumber = table.Column<int>(type: "int", nullable: true),
                    InternalPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InternalSuffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProtocolNumberFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumberPadding = table.Column<int>(type: "int", nullable: false),
                    AutoResetYearly = table.Column<bool>(type: "bit", nullable: false),
                    AllowManualEdit = table.Column<bool>(type: "bit", nullable: false),
                    ShowYearInNumber = table.Column<bool>(type: "bit", nullable: false),
                    UseSeparatorSlash = table.Column<bool>(type: "bit", nullable: false),
                    InstitutionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InstitutionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstitutionAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstitutionPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InstitutionEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstitutionWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FiscalYearStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiscalYearEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolSettings", x => x.ProtocolSettingsId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProtocolNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProtocolDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProtocolTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClassificationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    HasDeadline = table.Column<bool>(type: "bit", nullable: false),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsScanned = table.Column<bool>(type: "bit", nullable: false),
                    HasAttachments = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    SenderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SenderPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SenderEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SenderPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ReceivedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeliveryMethod = table.Column<int>(type: "int", nullable: true),
                    OriginalDocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalDocumentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresResponse = table.Column<bool>(type: "bit", nullable: true),
                    ResponseDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsResponded = table.Column<bool>(type: "bit", nullable: true),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseDocumentId = table.Column<int>(type: "int", nullable: true),
                    HasPhysicalCopy = table.Column<bool>(type: "bit", nullable: true),
                    PhysicalLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EnvelopeNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasSeal = table.Column<bool>(type: "bit", nullable: true),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: true),
                    DeliveryNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOriginal = table.Column<bool>(type: "bit", nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FromDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ToDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InternalType = table.Column<int>(type: "int", nullable: true),
                    FromUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CarbonCopyList = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalDocument_RequiresResponse = table.Column<bool>(type: "bit", nullable: true),
                    InternalDocument_ResponseDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InternalDocument_IsResponded = table.Column<bool>(type: "bit", nullable: true),
                    InternalDocument_ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InternalDocument_ResponseDocumentId = table.Column<int>(type: "int", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresSignature = table.Column<bool>(type: "bit", nullable: true),
                    SignedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HasDigitalSignature = table.Column<bool>(type: "bit", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: true),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequiresAttention = table.Column<bool>(type: "bit", nullable: true),
                    InternalDocument_IsConfidential = table.Column<bool>(type: "bit", nullable: true),
                    NumberOfCopies = table.Column<int>(type: "int", nullable: true),
                    InternalDocument_HasPhysicalCopy = table.Column<bool>(type: "bit", nullable: true),
                    InternalDocument_PhysicalLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCirculation = table.Column<bool>(type: "bit", nullable: true),
                    CirculationOrder = table.Column<int>(type: "int", nullable: true),
                    CirculationList = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedDocumentId = table.Column<int>(type: "int", nullable: true),
                    ActionRequired = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionCompleted = table.Column<bool>(type: "bit", nullable: true),
                    ActionCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutgoingDocument_InstitutionId = table.Column<int>(type: "int", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecipientPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RecipientAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    SentBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OutgoingDocument_DeliveryMethod = table.Column<int>(type: "int", nullable: true),
                    IsResponse = table.Column<bool>(type: "bit", nullable: true),
                    OriginalIncomingDocumentId = table.Column<int>(type: "int", nullable: true),
                    OutgoingDocument_SignedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SignerPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutgoingDocument_SignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutgoingDocument_HasDigitalSignature = table.Column<bool>(type: "bit", nullable: true),
                    IsSealed = table.Column<bool>(type: "bit", nullable: true),
                    OutgoingDocument_NumberOfCopies = table.Column<int>(type: "int", nullable: true),
                    RequiresDeliveryConfirmation = table.Column<bool>(type: "bit", nullable: true),
                    IsDeliveryConfirmed = table.Column<bool>(type: "bit", nullable: true),
                    ConfirmationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShipmentStatus = table.Column<int>(type: "int", nullable: true),
                    ShipmentNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShipmentCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ShipmentCompany = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasArchiveCopy = table.Column<bool>(type: "bit", nullable: true),
                    ArchiveLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OutgoingDocument_CarbonCopyList = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreparedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PreparedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_PreparedBy",
                        column: x => x.PreparedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_ReceivedBy",
                        column: x => x.ReceivedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_SentBy",
                        column: x => x.SentBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_SignedBy",
                        column: x => x.SignedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Documents_InternalDocument_ResponseDocumentId",
                        column: x => x.InternalDocument_ResponseDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Documents_OriginalIncomingDocumentId",
                        column: x => x.OriginalIncomingDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Documents_RelatedDocumentId",
                        column: x => x.RelatedDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Institutions_OutgoingDocument_InstitutionId",
                        column: x => x.OutgoingDocument_InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Deadlines",
                columns: table => new
                {
                    DeadlineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ResponsibleUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResponsibleDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    SendNotification = table.Column<bool>(type: "bit", nullable: false),
                    NotificationDaysBefore = table.Column<int>(type: "int", nullable: false),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SendReminder = table.Column<bool>(type: "bit", nullable: false),
                    ReminderIntervalDays = table.Column<int>(type: "int", nullable: false),
                    LastReminderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    AutoEscalate = table.Column<bool>(type: "bit", nullable: false),
                    EscalateToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EscalateDaysBefore = table.Column<int>(type: "int", nullable: false),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsExtended = table.Column<bool>(type: "bit", nullable: false),
                    OriginalDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtensionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtendedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExtensionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deadlines", x => x.DeadlineId);
                    table.ForeignKey(
                        name: "FK_Deadlines_AspNetUsers_CompletedBy",
                        column: x => x.CompletedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deadlines_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deadlines_AspNetUsers_EscalateToUserId",
                        column: x => x.EscalateToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deadlines_AspNetUsers_ResponsibleUserId",
                        column: x => x.ResponsibleUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deadlines_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimaryDocument = table.Column<bool>(type: "bit", nullable: false),
                    IsScanned = table.Column<bool>(type: "bit", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsCompressed = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PreviousVersionId = table.Column<int>(type: "int", nullable: true),
                    HasThumbnail = table.Column<bool>(type: "bit", nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    HasOCR = table.Column<bool>(type: "bit", nullable: false),
                    OCRText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OCRProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: false),
                    PrintCount = table.Column<int>(type: "int", nullable: false),
                    LastPrintedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDownloaded = table.Column<bool>(type: "bit", nullable: false),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    LastDownloadedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    LastViewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVirusScanned = table.Column<bool>(type: "bit", nullable: false),
                    IsVirusFree = table.Column<bool>(type: "bit", nullable: false),
                    VirusScanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AntivirusEngine = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAuthorization = table.Column<bool>(type: "bit", nullable: false),
                    AllowDownload = table.Column<bool>(type: "bit", nullable: false),
                    AllowPrint = table.Column<bool>(type: "bit", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAttachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_DocumentAttachments_AspNetUsers_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAttachments_DocumentAttachments_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "DocumentAttachments",
                        principalColumn: "AttachmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAttachments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTrackings",
                columns: table => new
                {
                    TrackingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    HasDeadline = table.Column<bool>(type: "bit", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresResponse = table.Column<bool>(type: "bit", nullable: false),
                    RequiresReport = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInProgress = table.Column<bool>(type: "bit", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletionPercentage = table.Column<int>(type: "int", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    RejectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDelegated = table.Column<bool>(type: "bit", nullable: false),
                    DelegatedToTrackingId = table.Column<int>(type: "int", nullable: true),
                    ParentTrackingId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsOverdue = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    AttachedFiles = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTrackings", x => x.TrackingId);
                    table.ForeignKey(
                        name: "FK_DocumentTrackings_AspNetUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTrackings_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTrackings_DocumentTrackings_DelegatedToTrackingId",
                        column: x => x.DelegatedToTrackingId,
                        principalTable: "DocumentTrackings",
                        principalColumn: "TrackingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTrackings_DocumentTrackings_ParentTrackingId",
                        column: x => x.ParentTrackingId,
                        principalTable: "DocumentTrackings",
                        principalColumn: "TrackingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTrackings_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Classifications",
                columns: new[] { "ClassificationId", "AllowCopy", "AllowDownload", "AllowPrint", "ColorCode", "CreatedBy", "CreatedDate", "Description", "EnableAuditLog", "IsActive", "IsDefault", "Level", "MinimumRoleRequired", "ModifiedBy", "ModifiedDate", "Name", "RequiresApproval", "RetentionYears", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, true, true, "#28a745", null, new DateTime(2025, 12, 28, 9, 45, 55, 22, DateTimeKind.Local).AddTicks(5727), "Dokumente publike që mund të shihen nga të gjithë", true, true, true, 1, null, null, null, "Publik", false, 5, 1 },
                    { 2, true, true, true, "#17a2b8", null, new DateTime(2025, 12, 28, 9, 45, 55, 22, DateTimeKind.Local).AddTicks(5740), "Vetëm për punonjësit e autorizuar", true, true, false, 2, null, null, null, "I Brendshëm", false, 10, 2 },
                    { 3, false, false, false, "#ffc107", null, new DateTime(2025, 12, 28, 9, 45, 55, 22, DateTimeKind.Local).AddTicks(5750), "Vetëm disa punonjës të caktuar", true, true, false, 3, null, null, null, "Konfidencial", true, 15, 3 },
                    { 4, false, false, false, "#fd7e14", null, new DateTime(2025, 12, 28, 9, 45, 55, 22, DateTimeKind.Local).AddTicks(5776), "Vetëm menaxherët dhe administratorët", true, true, false, 4, null, null, null, "Sekret", true, 20, 4 },
                    { 5, false, false, false, "#dc3545", null, new DateTime(2025, 12, 28, 9, 45, 55, 22, DateTimeKind.Local).AddTicks(5796), "Vetëm administratorët", true, true, false, 5, null, null, null, "Tepër Sekret", true, 30, 5 }
                });

            migrationBuilder.InsertData(
                table: "ProtocolSettings",
                columns: new[] { "ProtocolSettingsId", "AllowManualEdit", "AutoResetYearly", "ClosedBy", "ClosedDate", "CreatedBy", "CreatedDate", "FiscalYearEnd", "FiscalYearStart", "IncomingCurrentNumber", "IncomingEndNumber", "IncomingPrefix", "IncomingStartNumber", "IncomingSuffix", "InstitutionAddress", "InstitutionCode", "InstitutionEmail", "InstitutionName", "InstitutionPhone", "InstitutionWebsite", "InternalCurrentNumber", "InternalEndNumber", "InternalPrefix", "InternalStartNumber", "InternalSuffix", "IsActive", "IsClosed", "ModifiedBy", "ModifiedDate", "Notes", "NumberPadding", "OutgoingCurrentNumber", "OutgoingEndNumber", "OutgoingPrefix", "OutgoingStartNumber", "OutgoingSuffix", "ProtocolNumberFormat", "ShowYearInNumber", "UseSeparatorSlash", "Year" },
                values: new object[] { 1, false, true, null, null, null, new DateTime(2025, 12, 28, 9, 45, 55, 22, DateTimeKind.Local).AddTicks(6360), null, null, 1, null, "H", 1, null, null, null, null, null, null, null, 1, null, "B", 1, null, true, false, null, null, null, 4, 1, null, "D", 1, null, "{PREFIX}-{NUMBER}/{YEAR}", true, true, 2025 });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UserName",
                table: "AspNetUsers",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_Level",
                table: "Classifications",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_Name",
                table: "Classifications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_CompletedBy",
                table: "Deadlines",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_CreatedBy",
                table: "Deadlines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_DocumentId",
                table: "Deadlines",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_DueDate",
                table: "Deadlines",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_EscalateToUserId",
                table: "Deadlines",
                column: "EscalateToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_ResponsibleUserId",
                table: "Deadlines",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Deadlines_Status",
                table: "Deadlines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_DocumentId",
                table: "DocumentAttachments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_FileHash",
                table: "DocumentAttachments",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_PreviousVersionId",
                table: "DocumentAttachments",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_UploadedBy",
                table: "DocumentAttachments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAttachments_UploadedDate",
                table: "DocumentAttachments",
                column: "UploadedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ApprovedBy",
                table: "Documents",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AssignedTo",
                table: "Documents",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClassificationId",
                table: "Documents",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CreatedBy",
                table: "Documents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentType",
                table: "Documents",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FromDepartment",
                table: "Documents",
                column: "FromDepartment");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FromUserId",
                table: "Documents",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_InstitutionId",
                table: "Documents",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_InternalDocument_ResponseDocumentId",
                table: "Documents",
                column: "InternalDocument_ResponseDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OriginalIncomingDocumentId",
                table: "Documents",
                column: "OriginalIncomingDocumentId",
                unique: true,
                filter: "[OriginalIncomingDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_OutgoingDocument_InstitutionId",
                table: "Documents",
                column: "OutgoingDocument_InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PreparedBy",
                table: "Documents",
                column: "PreparedBy");

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
                name: "IX_Documents_ReceivedBy",
                table: "Documents",
                column: "ReceivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ReceivedDate",
                table: "Documents",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RelatedDocumentId",
                table: "Documents",
                column: "RelatedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SentBy",
                table: "Documents",
                column: "SentBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SentDate",
                table: "Documents",
                column: "SentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SignedBy",
                table: "Documents",
                column: "SignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status",
                table: "Documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ToDepartment",
                table: "Documents",
                column: "ToDepartment");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ToUserId",
                table: "Documents",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_AssignedByUserId",
                table: "DocumentTrackings",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_AssignedToUserId",
                table: "DocumentTrackings",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_DelegatedToTrackingId",
                table: "DocumentTrackings",
                column: "DelegatedToTrackingId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_DocumentId",
                table: "DocumentTrackings",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_DueDate",
                table: "DocumentTrackings",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_ParentTrackingId",
                table: "DocumentTrackings",
                column: "ParentTrackingId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTrackings_Status",
                table: "DocumentTrackings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Name",
                table: "Institutions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_TaxCode",
                table: "Institutions",
                column: "TaxCode",
                unique: true,
                filter: "[TaxCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolSettings_Year",
                table: "ProtocolSettings",
                column: "Year",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Deadlines");

            migrationBuilder.DropTable(
                name: "DocumentAttachments");

            migrationBuilder.DropTable(
                name: "DocumentTrackings");

            migrationBuilder.DropTable(
                name: "ProtocolSettings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Classifications");

            migrationBuilder.DropTable(
                name: "Institutions");
        }
    }
}
