using eProtokoll.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDocumentService
{
    // ================= CREATE =================

    Task<int> CreateIncomingAsync(
        IncomingDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId,
        string userName,
        CancellationToken cancellationToken = default);

    Task<int> CreateOutgoingAsync(
        OutgoingDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId,
        string userName,
        CancellationToken cancellationToken = default);

    Task<int> CreateInternalAsync(
        InternalDocument model,
        IFormFile? file,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int userId,
        string userName,
        CancellationToken cancellationToken = default);

    // ================= DETAILS =================

    Task<IncomingDocument?> GetIncomingByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OutgoingDocument?> GetOutgoingByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InternalDocument?> GetInternalByIdAsync(int id, CancellationToken cancellationToken = default);

    // ================= DROPDOWNS =================

    Task LoadDropdownsAsync(
        Action<SelectList> setInstitutions,
        Action<List<SelectListItem>> setClassifications,
        Action<IEnumerable<Users>> setUsers,
        bool isEmployee,
        CancellationToken cancellationToken = default);
}