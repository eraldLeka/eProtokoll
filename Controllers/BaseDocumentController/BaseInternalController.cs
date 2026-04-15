using eProtokoll.Models;
using eProtokoll.Repositories.Documents;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DocumentType = eProtokoll.Models.DocumentType;

namespace eProtokoll.Controllers.Base
{
    public abstract class BaseInternalDocumentController : Controller
    {
        protected readonly IDocumentRepository _documentRepository;
        protected readonly IProtocolNumberService _protocolNumberService;
        protected readonly IAuditLogRepository _auditLogRepository;
        protected readonly IDocumentFileService _documentFileService;

        protected virtual string AreaName => "Manager";

        protected BaseInternalDocumentController(
            IDocumentRepository documentRepository,
            IProtocolNumberService protocolNumberService,
            IAuditLogRepository auditLogRepository,
            IDocumentFileService documentFileService)
        {
            _documentRepository = documentRepository;
            _protocolNumberService = protocolNumberService;
            _auditLogRepository = auditLogRepository;
            _documentFileService = documentFileService;
        }

        // GET: Index
        public virtual async Task<IActionResult> Index(int page = 1)
        {
            ViewData["area"] = AreaName;

            var (documents, totalItems) =
                await _documentRepository.GetInternalAsync(page, 20);

            ViewBag.TotalInternal = totalItems;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)20);
            ViewBag.TotalItems = totalItems;

            return View("~/Views/InternalDocument/Index.cshtml", documents);
        }

        // GET: Create
        public virtual async Task<IActionResult> Create()
        {
            ViewData["area"] = AreaName;

            var document = new InternalDocument
            {
                Priority = Priority.Normal
            };

            await LoadDropdowns();
            return View("~/Views/InternalDocument/Create.cshtml", document);
        }

        // POST: Create (CLEAN VERSION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Create(
            InternalDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey)
        {
            ViewData["area"] = AreaName;

            var year = DateTime.Now.Year;

            model.DocumentNumber =
                await _protocolNumberService.GetNextDocumentNumberAsync(DocumentType.Internal, year);

            model.Year = year;

            ModelState.Remove(nameof(model.DocumentNumber));
            ModelState.Remove(nameof(model.Year));

            // business validation only
            if (model.Classification == Classification.Confidential &&
                (accessUserIds == null || accessUserIds.Count == 0))
            {
                ModelState.AddModelError("accessUserIds",
                    "Për klasifikimin 'I kufizuar' duhet të zgjidhni të paktën një përdorues.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                ViewBag.SelectedAccessUserIds = accessUserIds ?? new List<int>();
                return View("~/Views/InternalDocument/Create.cshtml", model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            model.CreatedDate = DateTime.Now;
            model.CreatedBy = userId;
            model.DocumentType = DocumentType.Internal;

            var documentId =
                await _documentRepository.InsertInternalAsync(model);

            // 🔥 FILE LOGIC MOVED OUT
            var attachment =
                await _documentFileService.ProcessFileAsync(
                    uploadFile: attachmentFile,
                    scanSessionKey: scanSessionKey,
                    documentId: documentId,
                    originalFileNameFallback: $"{model.Subject}.pdf",
                    contentType: "application/pdf",
                    userId: userId,
                    isSecret: model.Classification == Classification.Secret,
                    documentTypeFolder: "uploads/internal"
                );

            await _documentRepository.InsertAttachmentAsync(attachment);

            // permissions
            if (model.Classification == Classification.Confidential &&
                accessUserIds != null && accessUserIds.Count > 0)
            {
                await _documentRepository.InsertDocumentPermissionsAsync(documentId, accessUserIds);
            }

            // audit
            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = userId,
                UserName = User.Identity!.Name!,
                Action = "Create",
                DocumentId = documentId,
                Description = $"Krijoi dokument intern '{model.ProtocolNumber}'",
                Timestamp = DateTime.Now
            });

            TempData["SuccessMessage"] =
                $"Dokumenti intern '{model.ProtocolNumber}' u regjistrua me sukses!";

            return RedirectToAction(nameof(Index));
        }

        // GET: Details
        public virtual async Task<IActionResult> Details(int? id)
        {
            ViewData["area"] = AreaName;

            if (id == null)
                return NotFound();

            var document =
                await _documentRepository.GetInternalByIdAsync(id.Value);

            if (document == null)
                return NotFound();

            document.Attachments =
                await _documentRepository.GetAttachmentsByDocumentIdAsync(id.Value);

            return View("~/Views/InternalDocument/Details.cshtml", document);
        }

        // Dropdown helper (UNCHANGED)
        protected async Task LoadDropdowns(
            int? selectedClassificationId = null,
            bool isEmployee = false)
        {
            var classifications = Enum.GetValues(typeof(Classification))
                .Cast<Classification>()
                .Where(c => !isEmployee ||
                            c == Classification.Public ||
                            c == Classification.Confidential)
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.GetType()
                        .GetMember(c.ToString())
                        .FirstOrDefault()
                        ?.GetCustomAttribute<DisplayAttribute>()
                        ?.Name ?? c.ToString()
                }).ToList();

            ViewBag.Classifications = classifications;

            var users = await _documentRepository.GetActiveUsersAsync();
            ViewBag.AccessUsers = users;
        }
    }
}