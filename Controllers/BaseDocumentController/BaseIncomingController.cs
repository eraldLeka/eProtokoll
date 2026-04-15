using eProtokoll.Models;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using DocumentType = eProtokoll.Models.DocumentType;

namespace eProtokoll.Controllers.Base
{
    public abstract class BaseIncomingDocumentController : Controller
    {
        protected readonly IDocumentRepository _documentRepository;
        protected readonly IProtocolNumberService _protocolNumberService;
        protected readonly IAuditLogRepository _auditLogRepository;
        protected readonly IDocumentFileService _documentFileService;

        protected virtual string AreaName => "Manager";

        protected BaseIncomingDocumentController(
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
                await _documentRepository.GetIncomingAsync(page, 20);

            ViewBag.TotalIncoming = totalItems;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)20);
            ViewBag.TotalItems = totalItems;

            return View("~/Views/IncomingDocument/Index.cshtml", documents);
        }

        // GET: Create
        public virtual async Task<IActionResult> Create()
        {
            ViewData["area"] = AreaName;

            var document = new IncomingDocument
            {
                Priority = Priority.Normal
            };

            await LoadDropdowns();
            return View("~/Views/IncomingDocument/Create.cshtml", document);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Create(
            IncomingDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey)
        {
            ViewData["area"] = AreaName;

            var year = DateTime.Now.Year;

            model.DocumentNumber =
                await _protocolNumberService.GetNextDocumentNumberAsync(DocumentType.Incoming, year);

            model.Year = year;

            ModelState.Remove(nameof(model.DocumentNumber));
            ModelState.Remove(nameof(model.Year));

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
                return View("~/Views/IncomingDocument/Create.cshtml", model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            model.CreatedDate = DateTime.Now;
            model.CreatedBy = userId;
            model.DocumentType = DocumentType.Incoming;

            var documentId = await _documentRepository.InsertIncomingAsync(model);

            var attachment = await _documentFileService.ProcessFileAsync(
                uploadFile: attachmentFile,
                scanSessionKey: scanSessionKey,
                documentId: documentId,
                originalFileNameFallback: $"{model.Subject}.pdf",
                contentType: "application/pdf",
                userId: userId,
                isSecret: model.Classification == Classification.Secret,
                documentTypeFolder: "uploads/incoming"
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
                Description = $"Krijoi dokument hyrës '{model.ProtocolNumber}'",
                Timestamp = DateTime.Now
            });

            TempData["SuccessMessage"] =
                $"Dokumenti hyrës '{model.ProtocolNumber}' u regjistrua me sukses!";

            return RedirectToAction(nameof(Index));
        }

        // GET: Details
        public virtual async Task<IActionResult> Details(int? id)
        {
            ViewData["area"] = AreaName;

            if (id == null) return NotFound();

            var document = await _documentRepository.GetIncomingByIdAsync(id.Value);

            if (document == null) return NotFound();

            document.Attachments =
                await _documentRepository.GetAttachmentsByDocumentIdAsync(id.Value);

            return View("~/Views/IncomingDocument/Details.cshtml", document);
        }

        // Dropdown helper
        protected async Task LoadDropdowns(
            int? selectedInstitutionId = null,
            int? selectedClassificationId = null,
            bool isEmployee = false)
        {
            var institutions = await _documentRepository.GetInstitutionsAsync();

            ViewBag.Institutions = new SelectList(
                institutions, "InstitutionId", "Name", selectedInstitutionId);

            var classifications = Enum.GetValues(typeof(Classification))
                .Cast<Classification>()
                .Where(c => !isEmployee || c == Classification.Public || c == Classification.Confidential)
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