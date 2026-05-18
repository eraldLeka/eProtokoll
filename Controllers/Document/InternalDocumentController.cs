using eProtokoll.Models;
using eProtokoll.Repositories;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class InternalDocumentController : Controller
    {
        private readonly IDocumentService _service;
        private readonly IDocumentRepository _repo;
        private readonly ITrackingRepository _trackingRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public InternalDocumentController(
            IDocumentService service,
            IDocumentRepository repo,
            ITrackingRepository trackingRepository,
            IAuditLogRepository auditLogRepository)
        {
            _service = service;
            _repo = repo;
            _trackingRepository = trackingRepository;
            _auditLogRepository = auditLogRepository;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1)
        {
            var role = GetRole();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (documents, totalItems) = await _repo.GetInternalAsync(page, 20, userId, role);

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.Role = role;

            return View("~/Views/InternalDocument/Index.cshtml", documents);
        }

        // ================= CREATE (GET) =================
        [HttpGet]
        public async Task<IActionResult> Create(int? trackingId = null)
        {
            var role = GetRole();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            DocumentTracking? tracking = null;
            InternalDocument? original = null;

            if (trackingId.HasValue)
            {
                tracking = await ValidateTrackingForResponseAsync(
                    trackingId.Value,
                    userId,
                    DocumentType.Internal);

                if (tracking is null)
                    return Forbid();

                original = await _repo.GetInternalByIdAsync(tracking.DocumentId);
            }

            await LoadDropdowns(role, userId);

            var model = new InternalDocument
            {
                Priority = Priority.Normal,
                Classification = original?.Classification
                    ?? (role == "Employee" ? Classification.Public : Classification.Confidential),
                IsResponse = trackingId.HasValue,
                OriginalInternalDocumentId = tracking?.DocumentId,
                FromDepartment = original?.ToDepartment,
                ToDepartment = original?.FromDepartment
            };

            ViewBag.TrackingId = trackingId;
            ViewBag.OriginalInternalDisplay = original == null
                ? string.Empty
                : $"{original.DocumentNumber}/{original.Year} - {original.Subject}";
            return View("~/Views/InternalDocument/Create.cshtml", model);
        }

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            InternalDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey,
            int? trackingId)
        {
            var role = GetRole();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userName = User.Identity!.Name!;
            DocumentTracking? responseTracking = null;

            if (trackingId.HasValue)
            {
                responseTracking = await ValidateTrackingForResponseAsync(
                    trackingId.Value,
                    userId,
                    DocumentType.Internal);

                if (responseTracking is null)
                    return Forbid();

                model.IsResponse = true;
                model.OriginalInternalDocumentId = responseTracking.DocumentId;

                var original = await _repo.GetInternalByIdAsync(responseTracking.DocumentId);
                if (original != null)
                {
                    if (string.IsNullOrWhiteSpace(model.FromDepartment))
                        model.FromDepartment = original.ToDepartment;

                    if (string.IsNullOrWhiteSpace(model.ToDepartment))
                        model.ToDepartment = original.FromDepartment;
                }
            }

            if (role == "Employee" && model.Classification == Classification.Secret)
                ModelState.AddModelError("", "Employee nuk lejohet të krijojë Secret documents.");

            if (!ModelState.IsValid)
            {
                ViewBag.TrackingId = trackingId;
                await LoadDropdowns(role, userId);
                return View("~/Views/InternalDocument/Create.cshtml", model);
            }

            await _service.CreateInternalAsync(
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

                await _auditLogRepository.LogAsync(new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = "Respond",
                    DocumentId = responseTracking?.DocumentId,
                    Description = $"Iu përgjigj delegimit #{trackingId.Value} duke krijuar dokument të brendshëm për dokumentin #{responseTracking?.DocumentId}.",
                    Timestamp = DateTime.Now
                });
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

        // ================= ROLE HELPERS =================
        private string GetRole()
        {
            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }

        // ================= DROPDOWNS =================
        private async Task LoadDropdowns(string role, int userId)
        {
            var institutions = await _repo.GetInstitutionsAsync();
            var users = await _repo.GetActiveUsersAsync();
            var (internalDocs, _) = await _repo.GetInternalAsync(1, 2000, userId, role);

            var classifications = Enum.GetValues(typeof(Classification))
                .Cast<Classification>()
                .Where(c => role == "Admin" || role == "Manager" || c != Classification.Secret)
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
            ViewBag.Role = role;

            ViewBag.InternalDocuments = internalDocs
                .Where(d => d.RequiresResponse)
                .Select(d => new SelectListItem
                {
                    Value = d.DocumentId.ToString(),
                    Text = $"{d.DocumentNumber}/{d.Year} - {d.Subject}"
                })
                .ToList();
        }
    }
}