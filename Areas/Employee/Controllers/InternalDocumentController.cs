using eProtokoll.Controllers.Base;
using eProtokoll.Models;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DocumentType = eProtokoll.Models.DocumentType;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class InternalDocumentController : BaseInternalDocumentController
    {
        protected override string AreaName => "Employee";

        public InternalDocumentController(
            IDocumentRepository documentRepository,
            IWebHostEnvironment environment,
            IProtocolNumberService protocolNumberService,
            IAuditLogRepository auditLogRepository)
            : base(documentRepository, environment, protocolNumberService, auditLogRepository)
        {
        }

        // GET: Index — filtron vetëm dokumentet e këtij punonjësi
        public override async Task<IActionResult> Index(int page = 1)
        {
            ViewData["area"] = AreaName;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (documents, totalItems) = await _documentRepository
                .GetInternalAsync(page, 20, accessUserId: userId);

            ViewBag.TotalInternal = totalItems;
            ViewBag.TodayInternal =
                await _documentRepository.GetTodayCountAsync(DocumentType.Internal);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)20);
            ViewBag.TotalItems = totalItems;

            return View("~/Views/InternalDocument/Index.cshtml", documents);
        }

        // GET: Create
        public override async Task<IActionResult> Create()
        {
            ViewData["area"] = AreaName;
            var document = new InternalDocument { Priority = Priority.Normal };
            await LoadDropdowns(isEmployee: true);
            return View("~/Views/InternalDocument/Create.cshtml", document);
        }

        // POST: Create — bllokon Secret nga server
        [HttpPost]
        [ValidateAntiForgeryToken]
        public override async Task<IActionResult> Create(
            InternalDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds)
        {
            if (model.Classification == Classification.Secret)
            {
                ModelState.AddModelError("Classification",
                    "Punonjësit nuk lejohen të regjistrojnë dokumente sekrete.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["area"] = AreaName;
                await LoadDropdowns(isEmployee: true);
                ViewBag.SelectedAccessUserIds = accessUserIds ?? new List<int>();
                return View("~/Views/InternalDocument/Create.cshtml", model);
            }

            return await base.Create(model, attachmentFile, accessUserIds);
        }
    }
}