using eProtokoll.Controllers.Base;
using eProtokoll.Models;
using eProtokoll.Repositories.Document;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DocumentType = eProtokoll.Models.DocumentType;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class OutgoingDocumentController : BaseOutgoingDocumentController
    {
        protected override string AreaName => "Employee";

        public OutgoingDocumentController(
            IDocumentRepository documentRepository,
            IWebHostEnvironment environment,
            IProtocolNumberService protocolNumberService)
            : base(documentRepository, environment, protocolNumberService)
        {
        }

        // GET: Index
        public override async Task<IActionResult> Index(int page = 1)
        {
            ViewData["area"] = AreaName;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (documents, totalItems) = await _documentRepository
                .GetOutgoingAsync(page, 20, accessUserId: userId);

            ViewBag.TotalOutgoing = totalItems;
            ViewBag.TodayOutgoing = await _documentRepository
                .GetTodayCountAsync(DocumentType.Outgoing);
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)20);
            ViewBag.TotalItems = totalItems;

            return View("~/Views/OutgoingDocument/Index.cshtml", documents);
        }

        // GET: Create
        public override async Task<IActionResult> Create()
        {
            ViewData["area"] = AreaName;
            var document = new OutgoingDocument { Priority = Priority.Normal };
            await LoadDropdowns(isEmployee: true);
            return View("~/Views/OutgoingDocument/Create.cshtml", document);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public override async Task<IActionResult> Create(
            OutgoingDocument model,
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
                return View("~/Views/OutgoingDocument/Create.cshtml", model);
            }

            return await base.Create(model, attachmentFile, accessUserIds);
        }
    }
}