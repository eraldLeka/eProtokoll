using eProtokoll.Models;
using eProtokoll.Repositories;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

[Authorize(Roles = "Employee,Manager,Admin")]
public class OutgoingDocumentController : Controller
{
    private readonly IDocumentService _service;
    private readonly IDocumentRepository _repo;
    private readonly ITrackingRepository _trackingRepository;
    public OutgoingDocumentController(
        IDocumentService service,
        IDocumentRepository repo,
        ITrackingRepository trackingRepository)
    {
        _service = service;
        _repo = repo;
        _trackingRepository = trackingRepository;
    }

    // ================= INDEX =================
    public async Task<IActionResult> Index(int page = 1)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var (documents, totalItems) =
            await _repo.GetOutgoingAsync(page, 20, userId, role);

        ViewBag.CurrentPage = page;
        ViewBag.TotalItems = totalItems;
        ViewBag.Role = role;

        return View("~/Views/OutgoingDocument/Index.cshtml", documents);
    }

    // ================= CREATE (GET) =================
    public async Task<IActionResult> Create(int? trackingId = null, int? originalIncomingDocumentId = null)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        bool isEmployee = role == "Employee";

        if (trackingId.HasValue)
        {
            var tracking = await ValidateTrackingForResponseAsync(
                trackingId.Value,
                userId,
                DocumentType.Incoming);

            if (tracking is null)
                return Forbid();

            originalIncomingDocumentId ??= tracking.DocumentId;
        }

        await LoadDropdowns(isEmployee, userId, role);

        IncomingDocument? original = null;
        if (originalIncomingDocumentId.HasValue)
            original = await _repo.GetIncomingByIdAsync(originalIncomingDocumentId.Value);

        var model = new OutgoingDocument
        {
            Priority = Priority.Normal,
            IsResponse = trackingId.HasValue,
            OriginalIncomingDocumentId = originalIncomingDocumentId,
            InstitutionId = original?.InstitutionId ?? 0,
            RecipientName = original?.SenderName ?? string.Empty,
            Classification = original?.Classification ?? Classification.Public
        };

        ViewBag.TrackingId = trackingId;
        ViewBag.OriginalIncomingDisplay = original == null
            ? string.Empty
            : $"{original.DocumentNumber}/{original.Year} - {original.Subject}";
        return View("~/Views/OutgoingDocument/Create.cshtml", model);
    }

    // ================= CREATE (POST) =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        OutgoingDocument model,
        IFormFile? attachmentFile,
        List<int>? accessUserIds,
        string? scanSessionKey,
        int? trackingId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userName = User.Identity!.Name!;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";

        if (trackingId.HasValue)
        {
            var tracking = await ValidateTrackingForResponseAsync(
                trackingId.Value,
                userId,
                DocumentType.Incoming);

            if (tracking is null)
                return Forbid();

            model.IsResponse = true;
            model.OriginalIncomingDocumentId = tracking.DocumentId;

            var original = await _repo.GetIncomingByIdAsync(tracking.DocumentId);
            if (original != null)
            {
                if (model.InstitutionId == 0)
                    model.InstitutionId = original.InstitutionId;

                if (string.IsNullOrWhiteSpace(model.RecipientName))
                    model.RecipientName = original.SenderName;
            }
        }

        // RULE: Employee nuk krijon Secret
        if (role == "Employee" && model.Classification == Classification.Secret)
        {
            ModelState.AddModelError(
                nameof(model.Classification),
                "Employee nuk lejohet të krijojë dokumente sekrete."
            );
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TrackingId = trackingId;
            await LoadDropdowns(role == "Employee", userId, role);
            return View("~/Views/OutgoingDocument/Create.cshtml", model);
        }

        await _service.CreateOutgoingAsync(
            model,
            attachmentFile,
            accessUserIds,
            scanSessionKey,
            userId,
            userName
        );

        if (trackingId.HasValue)
        {
            await _trackingRepository.CompleteAsync(trackingId.Value);

        }

        TempData["SuccessMessage"] = "Dokumenti u krijua me sukses.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<DocumentTracking?> ValidateTrackingForResponseAsync(
        int trackingId,
        int userId,
        DocumentType expectedType)
    {
        var tracking = await _trackingRepository.GetByIdAsync(trackingId);

        if (tracking is null)
            return null;

        if (tracking.AssignedToUserId != userId)
            return null;

        if (tracking.CompletedDate.HasValue)
            return null;

        if (tracking.DocumentType != expectedType)
            return null;

        return tracking;
    }

    // ================= DROPDOWNS =================
    private async Task LoadDropdowns(bool isEmployee, int userId, string role)
    {
        var institutions = await _repo.GetInstitutionsAsync();
        var users = await _repo.GetActiveUsersAsync();
        var (incomingDocuments, _) = await _repo.GetIncomingAsync(1, 2000, userId, role);

        var classifications = Enum.GetValues(typeof(Classification))
            .Cast<Classification>()
            .Where(c => !isEmployee ||
                        c == Classification.Public ||
                        c == Classification.Confidential)
            .Select(c => new SelectListItem
            {
                Value = ((int)c).ToString(),
                Text = c switch
                {
                    Classification.Public => "Publik",
                    Classification.Confidential => "I kufizuar",
                    Classification.Secret => "Sekret",
                    _ => c.ToString()
                }
            })
            .ToList();

        ViewBag.Institutions = new SelectList(institutions, "InstitutionId", "Name");
        ViewBag.Classifications = classifications;
        ViewBag.AccessUsers = users;
        ViewBag.IncomingDocuments = incomingDocuments
            .Select(d => new SelectListItem
            {
                Value = d.DocumentId.ToString(),
                Text = $"{d.DocumentNumber}/{d.Year} - {d.Subject}"
            })
            .ToList();
    }
}