using eProtokoll.Models;
using eProtokoll.Repositories.Document;
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
    public abstract class BaseOutgoingDocumentController : Controller
    {
        protected readonly IDocumentRepository _documentRepository;
        protected readonly IWebHostEnvironment _environment;
        protected readonly IProtocolNumberService _protocolNumberService;
        protected readonly FileService _fileService;

        protected virtual string AreaName => "Manager";

        protected BaseOutgoingDocumentController(
            IDocumentRepository documentRepository,
            IWebHostEnvironment environment,
            IProtocolNumberService protocolNumberService)
        {
            _documentRepository = documentRepository;
            _environment = environment;
            _protocolNumberService = protocolNumberService;

            var uploadsFolder = Path.Combine(environment.WebRootPath, "uploads", "outgoing");
            _fileService = new FileService(uploadsFolder);
        }

        // ==================== INDEX ====================

        public virtual async Task<IActionResult> Index(int page = 1)
        {
            ViewData["area"] = AreaName;

            var (documents, totalItems) = await _documentRepository.GetOutgoingAsync(page, 20);

            ViewBag.TotalOutgoing = totalItems;
            ViewBag.TodayOutgoing = await _documentRepository.GetTodayCountAsync(DocumentType.Outgoing);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)20);
            ViewBag.TotalItems = totalItems;

            return View("~/Views/OutgoingDocument/Index.cshtml", documents);
        }

        // ==================== CREATE (GET) ====================

        public virtual async Task<IActionResult> Create()
        {
            ViewData["area"] = AreaName;

            var document = new OutgoingDocument
            {
                Priority = Priority.Normal
            };

            await LoadDropdowns();
            return View("~/Views/OutgoingDocument/Create.cshtml", document);
        }

        // ==================== CREATE (POST) ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Create(
            OutgoingDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds)
        {
            ViewData["area"] = AreaName;

            var protocolNumber = await _protocolNumberService
                .GenerateNextProtocolNumberAsync(
                    eProtokoll.Services.ProtocolNumber.DocumentType.Outgoing);

            model.ProtocolNumber = protocolNumber;

            var now = DateTime.Now;
            model.ProtocolDate = now.Date;
            model.ProtocolTime = new TimeSpan(now.Hour, now.Minute, now.Second);
            model.Status = DocumentStatus.Registered;
            model.Priority = Priority.Normal;

            ModelState.Remove(nameof(model.ProtocolNumber));
            ModelState.Remove(nameof(model.ProtocolDate));
            ModelState.Remove(nameof(model.ProtocolTime));

            if (attachmentFile == null || attachmentFile.Length == 0)
                ModelState.AddModelError("attachmentFile", "Ngarko PDF për dokumentin dalës.");
            else if (Path.GetExtension(attachmentFile.FileName).ToLower() != ".pdf")
                ModelState.AddModelError("attachmentFile", "Vetëm PDF lejohet.");

            // Validim: nëse Confidential, duhet të paktën 1 user
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
                model.DocumentType = DocumentType.Outgoing;

                var documentId = await _documentRepository.InsertOutgoingAsync(model);

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
                    savedFile.DisplayOrder = 1;
                    savedFile.IsPrimaryDocument = true;

                    await _documentRepository.InsertAttachmentAsync(savedFile);
                }

                // Nëse Confidential, ruaj access users
                if (model.Classification == Classification.Confidential &&
                    accessUserIds != null && accessUserIds.Count > 0)
                {
                    await _documentRepository.InsertDocumentPermissionsAsync(documentId, accessUserIds);
                }

                TempData["SuccessMessage"] =
                    $"Dokumenti dalës '{model.ProtocolNumber}' u regjistrua me sukses!";

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            ViewBag.SelectedAccessUserIds = accessUserIds ?? new List<int>();
            return View("~/Views/OutgoingDocument/Create.cshtml", model);
        }

        // ==================== DETAILS ====================

        public virtual async Task<IActionResult> Details(int? id)
        {
            ViewData["area"] = AreaName;

            if (id == null)
                return NotFound();

            var document = await _documentRepository.GetOutgoingByIdAsync(id.Value);

            if (document == null)
                return NotFound();

            document.Attachments =
                await _documentRepository.GetAttachmentsByDocumentIdAsync(id.Value);

            return View("~/Views/OutgoingDocument/Details.cshtml", document);
        }

        // ==================== DROPDOWNS ====================

        protected async Task LoadDropdowns(
            int? selectedInstitutionId = null,
            int? selectedClassificationId = null,
            bool isEmployee = false)
        {
            var institutions = await _documentRepository.GetInstitutionsAsync();

            ViewBag.Institutions = new SelectList(
                institutions,
                "InstitutionId",
                "Name",
                selectedInstitutionId);

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