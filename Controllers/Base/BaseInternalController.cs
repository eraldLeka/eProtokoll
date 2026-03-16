using eProtokoll.Models;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DocumentType = eProtokoll.Models.DocumentType;
using eProtokoll.Repositories.AuditLogs;

namespace eProtokoll.Controllers.Base
{
    public abstract class BaseInternalDocumentController : Controller
    {
        protected readonly IDocumentRepository _documentRepository;
        protected readonly IWebHostEnvironment _environment;
        protected readonly IProtocolNumberService _protocolNumberService;
        protected readonly FileService _fileService;
        protected readonly IAuditLogRepository _auditLogRepository;

        protected virtual string AreaName => "Manager";

        protected BaseInternalDocumentController(
            IDocumentRepository documentRepository,
            IWebHostEnvironment environment,
            IProtocolNumberService protocolNumberService,
            IAuditLogRepository auditLogRepository)
        {
            _documentRepository = documentRepository;
            _environment = environment;
            _protocolNumberService = protocolNumberService;
            _auditLogRepository = auditLogRepository;

            var uploadsFolder = Path.Combine(environment.WebRootPath, "uploads", "internal");
            _fileService = new FileService(uploadsFolder);
        }

        // GET: Index
        public virtual async Task<IActionResult> Index(int page = 1)
        {
            ViewData["area"] = AreaName;

            var (documents, totalItems) = await _documentRepository.GetInternalAsync(page, 20);

            ViewBag.TotalInternal = totalItems;
            ViewBag.TodayInternal = await _documentRepository.GetTodayCountAsync(DocumentType.Internal);
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

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Create(
            InternalDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds)
        {
            ViewData["area"] = AreaName;

            var year = DateTime.Now.Year;
            model.DocumentNumber = await _protocolNumberService
                .GetNextDocumentNumberAsync(DocumentType.Internal, year);
            model.Year = year;
            model.Priority = Priority.Normal;

            ModelState.Remove(nameof(model.DocumentNumber));
            ModelState.Remove(nameof(model.Year));

            if (attachmentFile == null || attachmentFile.Length == 0)
                ModelState.AddModelError("attachmentFile", "Ngarko pdf per dokumentin intern.");
            else if (Path.GetExtension(attachmentFile.FileName).ToLower() != ".pdf")
                ModelState.AddModelError("attachmentFile", "Vetëm PDF lejohet.");

            if (model.Classification == Classification.Confidential &&
                (accessUserIds == null || accessUserIds.Count == 0))
            {
                ModelState.AddModelError("accessUserIds",
                    "Për klasifikimin 'I kufizuar' duhet të zgjidhni të paktën një përdorues.");
            }

            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                model.CreatedDate = DateTime.Now;
                model.CreatedBy = userId;
                model.DocumentType = DocumentType.Internal;

                var documentId = await _documentRepository.InsertInternalAsync(model);

                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await attachmentFile.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();

                    var savedFile = _fileService.SaveFile(
                        fileBytes,
                        attachmentFile.FileName,
                        documentId,
                        attachmentFile.ContentType,
                        userId);

                    savedFile.Category = FileCategory.PDF;

                    await _documentRepository.InsertAttachmentAsync(savedFile);
                }

                if (model.Classification == Classification.Confidential &&
                    accessUserIds != null && accessUserIds.Count > 0)
                {
                    await _documentRepository.InsertDocumentPermissionsAsync(documentId, accessUserIds);
                }

                TempData["SuccessMessage"] =
                    $"Dokumenti intern '{model.ProtocolNumber}' u regjistrua me sukses!";

                await _auditLogRepository.LogAsync(new AuditLog
                {
                    UserId = (int)model.CreatedBy,
                    UserName = User.Identity!.Name!,
                    Action = "Create",
                    DocumentId = documentId,
                    Description = $"Krijoi dokument intern '{model.ProtocolNumber}'",
                    Timestamp = DateTime.Now
                });

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            ViewBag.SelectedAccessUserIds = accessUserIds ?? new List<int>();
            return View("~/Views/InternalDocument/Create.cshtml", model);
        }

        // GET: Details
        public virtual async Task<IActionResult> Details(int? id)
        {
            ViewData["area"] = AreaName;

            if (id == null)
                return NotFound();

            var document = await _documentRepository.GetInternalByIdAsync(id.Value);

            if (document == null)
                return NotFound();

            document.Attachments =
                await _documentRepository.GetAttachmentsByDocumentIdAsync(id.Value);

            return View("~/Views/InternalDocument/Details.cshtml", document);
        }

        // Dropdown helper
        protected async Task LoadDropdowns(
            int? selectedClassificationId = null,
            bool isEmployee = false)
        {
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