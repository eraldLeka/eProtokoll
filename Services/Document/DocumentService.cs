using eProtokoll.Models;
using eProtokoll.Repositories.Documents;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

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

    // ================= LISTING =================

    public async Task<(IEnumerable<IncomingDocument> Documents, int TotalItems)> GetIncomingListAsync(int page, int pageSize)
    {
        var (docs, total) = await _repo.GetIncomingAsync(page, pageSize);
        return (docs, total);
    }

    public async Task<(IEnumerable<OutgoingDocument> Documents, int TotalItems)> GetOutgoingListAsync(int page, int pageSize)
    {
        var (docs, total) = await _repo.GetOutgoingAsync(page, pageSize);
        return (docs, total);
    }

    public async Task<(IEnumerable<InternalDocument> Documents, int TotalItems)> GetInternalListAsync(int page, int pageSize)
    {
        var (docs, total) = await _repo.GetInternalAsync(page, pageSize);
        return (docs, total);
    }

    // ================= CREATE =================

    public Task<int> CreateIncomingAsync(
        IncomingDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId)
        => CreateCoreAsync(model, file, accessUserIds, scanSessionKey, userId, DocumentType.Incoming);

    public Task<int> CreateOutgoingAsync(
        OutgoingDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId)
        => CreateCoreAsync(model, file, accessUserIds, scanSessionKey, userId, DocumentType.Outgoing);

    public Task<int> CreateInternalAsync(
        InternalDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId)
        => CreateCoreAsync(model, file, accessUserIds, scanSessionKey, userId, DocumentType.Internal);

    // ================= CORE =================

    private async Task<int> CreateCoreAsync<T>(
        T model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId,
        DocumentType type) where T : Document
    {
        var year = DateTime.Now.Year;
        var number = await _protocol.GetNextDocumentNumberAsync(type, year);

        model.DocumentNumber = number;
        model.Year = year;
        model.CreatedDate = DateTime.Now;
        model.CreatedBy = userId;
        model.DocumentType = type;

        var subject = model.Subject;
        var classification = model.Classification;

        int documentId = type switch
        {
            DocumentType.Incoming => await _repo.InsertIncomingAsync((IncomingDocument)(object)model),
            DocumentType.Outgoing => await _repo.InsertOutgoingAsync((OutgoingDocument)(object)model),
            DocumentType.Internal => await _repo.InsertInternalAsync((InternalDocument)(object)model),
            _ => throw new ArgumentOutOfRangeException()
        };

        var attachment = await _file.ProcessFileAsync(
            uploadFile: file,
            scanSessionKey: scanSessionKey,
            documentId: documentId,
            originalFileNameFallback: $"{subject}.pdf",
            contentType: "application/pdf",
            userId: userId,
            isSecret: classification == Classification.Secret,
            documentTypeFolder: type switch
            {
                DocumentType.Incoming => "uploads/incoming",
                DocumentType.Outgoing => "uploads/outgoing",
                DocumentType.Internal => "uploads/internal",
                _ => "uploads"
            }
        );

        await _repo.InsertAttachmentAsync(attachment);

        if (classification == Classification.Confidential &&
            accessUserIds != null && accessUserIds.Count > 0)
        {
            await _repo.InsertDocumentPermissionsAsync(documentId, accessUserIds);
        }

        await _audit.LogAsync(new AuditLog
        {
            UserId = userId,
            UserName = "system",
            Action = "Create",
            DocumentId = documentId,
            Description = $"Created {type} document '{number}'",
            Timestamp = DateTime.Now
        });

        return documentId;
    }

    // ================= DETAILS =================

    public Task<IncomingDocument?> GetIncomingByIdAsync(int id)
        => _repo.GetIncomingByIdAsync(id);

    public Task<OutgoingDocument?> GetOutgoingByIdAsync(int id)
        => _repo.GetOutgoingByIdAsync(id);

    public Task<InternalDocument?> GetInternalByIdAsync(int id)
        => _repo.GetInternalByIdAsync(id);

    // ================= DROPDOWNS =================

    public async Task LoadDropdownsAsync(
        Action<SelectList> setInstitutions,
        Action<List<SelectListItem>> setClassifications,
        Action<IEnumerable<object>> setUsers,
        bool isEmployee)
    {
        var institutions = await _repo.GetInstitutionsAsync();
        var users = await _repo.GetActiveUsersAsync();

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
}