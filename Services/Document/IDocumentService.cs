using eProtokoll.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

public interface IDocumentService
{
    // ================= LISTING =================
    Task<(IEnumerable<IncomingDocument> Documents, int TotalItems)> GetIncomingListAsync(int page, int pageSize);

    Task<(IEnumerable<OutgoingDocument> Documents, int TotalItems)> GetOutgoingListAsync(int page, int pageSize);

    Task<(IEnumerable<InternalDocument> Documents, int TotalItems)> GetInternalListAsync(int page, int pageSize);

    // ================= CREATE =================
    Task<int> CreateIncomingAsync(
        IncomingDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId);

    Task<int> CreateOutgoingAsync(
        OutgoingDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId);

    Task<int> CreateInternalAsync(
        InternalDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId);

    // ================= DETAILS =================
    Task<IncomingDocument?> GetIncomingByIdAsync(int id);

    Task<OutgoingDocument?> GetOutgoingByIdAsync(int id);

    Task<InternalDocument?> GetInternalByIdAsync(int id);

    // ================= DROPDOWNS =================
    Task LoadDropdownsAsync(
        Action<SelectList> setInstitutions,
        Action<List<SelectListItem>> setClassifications,
        Action<IEnumerable<object>> setUsers,
        bool isEmployee);
}