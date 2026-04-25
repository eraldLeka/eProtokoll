using eProtokoll.Models;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repo;
    private readonly IProtocolNumberService _protocol;
    private readonly IDocumentFileService _file;
    private readonly IAuditLogRepository _audit;

    public DocumentService(
        IDocumentRepository repo,
        IProtocolNumberService protocol,
        IDocumentFileService file,
        IAuditLogRepository audit)
    {
        _repo = repo;
        _protocol = protocol;
        _file = file;
        _audit = audit;
    }

    // ================= CREATE WRAPPERS =================

    public Task<int> CreateIncomingAsync(IncomingDocument model, IFormFile? file,
        List<int>? accessUserIds, string? scanSessionKey, int userId, string userName,
        CancellationToken cancellationToken = default)
        => CreateCoreAsync(model, file, accessUserIds, scanSessionKey, userId, userName, DocumentType.Incoming);

    public Task<int> CreateOutgoingAsync(OutgoingDocument model, IFormFile? file,
        List<int>? accessUserIds, string? scanSessionKey, int userId, string userName,
        CancellationToken cancellationToken = default)
        => CreateCoreAsync(model, file, accessUserIds, scanSessionKey, userId, userName, DocumentType.Outgoing);

    public Task<int> CreateInternalAsync(InternalDocument model, IFormFile? file,
        List<int>? accessUserIds, string? scanSessionKey, int userId, string userName,
        CancellationToken cancellationToken = default)
        => CreateCoreAsync(model, file, accessUserIds, scanSessionKey, userId, userName, DocumentType.Internal);

    // ================= DETAILS =================

    public Task<IncomingDocument?> GetIncomingByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repo.GetIncomingByIdAsync(id);

    public Task<OutgoingDocument?> GetOutgoingByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repo.GetOutgoingByIdAsync(id);

    public Task<InternalDocument?> GetInternalByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repo.GetInternalByIdAsync(id);

    // ================= CORE =================

    private async Task<int> CreateCoreAsync<T>(
        T model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId,
        string userName,
        DocumentType type) where T : Document
    {
        var year = DateTime.UtcNow.Year;
        var number = await _protocol.GetNextDocumentNumberAsync(type, year);

        model.DocumentNumber = number;
        model.Year = year;
        model.CreatedDate = DateTime.UtcNow;
        model.CreatedBy = userId;
        model.DocumentType = type;
        model.HasAttachments = file != null || !string.IsNullOrEmpty(scanSessionKey);

        // ================= INSERT =================
        var documentId = await _repo.InsertAsync(
            @"INSERT INTO Documents
              (DocumentNumber, Year, DocumentType, Subject, Content,
               Classification, Priority, RequiresResponse, HasAttachments, CreatedDate, CreatedBy,
               InstitutionId, SenderName, ReceivedDate, ReceivedBy,
               OriginalDocumentNumber, OriginalDocumentDate, ResponseDeadline, ResponseDate, ResponseDocumentId,
               RecipientName, IsResponse, OriginalIncomingDocumentId, ArchiveLocation,
               FromDepartment, ToDepartment)
              OUTPUT INSERTED.DocumentId
              VALUES
              (@DocumentNumber, @Year, @DocumentType, @Subject, @Content,
               @Classification, @Priority, @RequiresResponse, @HasAttachments, @CreatedDate, @CreatedBy,
               @InstitutionId, @SenderName, @ReceivedDate, @ReceivedBy,
               @OriginalDocumentNumber, @OriginalDocumentDate, @ResponseDeadline, @ResponseDate, @ResponseDocumentId,
               @RecipientName, @IsResponse, @OriginalIncomingDocumentId, @ArchiveLocation,
               @FromDepartment, @ToDepartment)",
            DocumentToSql(model)
        );

        // ================= FILE =================
        if (file != null || !string.IsNullOrEmpty(scanSessionKey))
        {
            var attachment = await _file.ProcessFileAsync(
                uploadFile: file,
                scanSessionKey: scanSessionKey,
                documentId: documentId,
                originalFileNameFallback: $"{model.Subject}.pdf",
                contentType: "application/pdf",
                userId: userId,
                isSecret: model.Classification == Classification.Secret,
                documentTypeFolder: GetFolder(type)
            );

            await _repo.InsertAttachmentAsync(
                @"INSERT INTO DocumentAttachments
                  (DocumentId, OriginalFileName, FilePath, FileSize,
                   FileExtension, UploadedDate, UploadedBy, Category, FileHash)
                  VALUES
                  (@DocumentId, @OriginalFileName, @FilePath, @FileSize,
                   @FileExtension, @UploadedDate, @UploadedBy, @Category, @FileHash)",
                AttachmentToSql(attachment)
            );
        }

        // ================= PERMISSIONS =================
        if (model.Classification == Classification.Confidential &&
            accessUserIds?.Any() == true)
        {
            await _repo.InsertDocumentPermissionsAsync(documentId, accessUserIds);
        }

        // ================= AUDIT =================
        await _audit.LogAsync(new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = "Create",
            DocumentId = documentId,
            Description = $"Krijoi dokument {type} '{number}/{year}'",
            Timestamp = DateTime.UtcNow
        });

        return documentId;
    }

    // ================= DROPDOWNS =================

    public async Task LoadDropdownsAsync(
        Action<SelectList> setInstitutions,
        Action<List<SelectListItem>> setClassifications,
        Action<IEnumerable<Users>> setUsers,
        bool isEmployee,
        CancellationToken cancellationToken = default)
    {
        var institutions = await _repo.GetInstitutionsAsync();

        var users = (await _repo.GetActiveUsersAsync())
            .Where(u => u.Role == Users.UserRole.Employee)
            .ToList();

        var classifications = Enum.GetValues(typeof(Classification))
            .Cast<Classification>()
            .Where(c => !isEmployee ||
                        c == Classification.Public ||
                        c == Classification.Confidential)
            .Select(c => new SelectListItem
            {
                Value = ((int)c).ToString(),
                Text = c.ToString()
            })
            .ToList();

        setInstitutions(new SelectList(institutions, "InstitutionId", "Name"));
        setClassifications(classifications);
        setUsers(users);
    }

    // ================= HELPERS =================

    private string GetFolder(DocumentType type) => type switch
    {
        DocumentType.Incoming => "incoming",
        DocumentType.Outgoing => "outgoing",
        DocumentType.Internal => "internal",
        _ => string.Empty
    };

    private SqlParameter[] DocumentToSql(Document model)
    {
        var incoming = model as IncomingDocument;
        var outgoing = model as OutgoingDocument;
        var internalDoc = model as InternalDocument;

        var institutionId = incoming?.InstitutionId ?? outgoing?.InstitutionId;

        return new[]
        {
            new SqlParameter("@DocumentNumber", model.DocumentNumber),
            new SqlParameter("@Year", model.Year),
            new SqlParameter("@DocumentType", (int)model.DocumentType),
            new SqlParameter("@Subject", model.Subject),
            new SqlParameter("@Content", (object?)model.Content ?? DBNull.Value),
            new SqlParameter("@Classification", (int)model.Classification),
            new SqlParameter("@Priority", (int)model.Priority),
            new SqlParameter("@RequiresResponse", model.RequiresResponse),
            new SqlParameter("@HasAttachments", model.HasAttachments),
            new SqlParameter("@CreatedDate", model.CreatedDate),
            new SqlParameter("@CreatedBy", model.CreatedBy),

            new SqlParameter("@InstitutionId", (object?)institutionId ?? DBNull.Value),
            new SqlParameter("@SenderName", (object?)incoming?.SenderName ?? DBNull.Value),
            new SqlParameter("@ReceivedDate", (object?)incoming?.ReceivedDate ?? DBNull.Value),
            new SqlParameter("@ReceivedBy", (object?)incoming?.ReceivedBy ?? DBNull.Value),
            new SqlParameter("@OriginalDocumentNumber", (object?)incoming?.OriginalDocumentNumber ?? DBNull.Value),
            new SqlParameter("@OriginalDocumentDate", (object?)incoming?.OriginalDocumentDate ?? DBNull.Value),
            new SqlParameter("@ResponseDeadline", (object?)incoming?.ResponseDeadline ?? DBNull.Value),
            new SqlParameter("@ResponseDate", (object?)incoming?.ResponseDate ?? DBNull.Value),
            new SqlParameter("@ResponseDocumentId", (object?)incoming?.ResponseDocumentId ?? DBNull.Value),

            new SqlParameter("@RecipientName", (object?)outgoing?.RecipientName ?? DBNull.Value),
            new SqlParameter("@IsResponse", (object?)outgoing?.IsResponse ?? DBNull.Value),
            new SqlParameter("@OriginalIncomingDocumentId", (object?)outgoing?.OriginalIncomingDocumentId ?? DBNull.Value),
            new SqlParameter("@ArchiveLocation", (object?)outgoing?.ArchiveLocation ?? DBNull.Value),

            new SqlParameter("@FromDepartment", (object?)internalDoc?.FromDepartment ?? DBNull.Value),
            new SqlParameter("@ToDepartment", (object?)internalDoc?.ToDepartment ?? DBNull.Value)
        };
    }

    private SqlParameter[] AttachmentToSql(DocumentAttachment a) => new[]
    {
        new SqlParameter("@DocumentId", a.DocumentId),
        new SqlParameter("@OriginalFileName", a.OriginalFileName),
        new SqlParameter("@FilePath", a.FilePath),
        new SqlParameter("@FileSize", a.FileSize),
        new SqlParameter("@FileExtension", (object?)a.FileExtension ?? DBNull.Value),
        new SqlParameter("@UploadedDate", a.UploadedDate),
        new SqlParameter("@UploadedBy", a.UploadedBy),
        new SqlParameter("@Category", (int)a.Category),
        new SqlParameter("@FileHash", (object?)a.FileHash ?? DBNull.Value)
    };
}