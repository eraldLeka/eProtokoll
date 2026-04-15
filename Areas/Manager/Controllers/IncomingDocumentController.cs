using eProtokoll.Models;
using eProtokoll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class IncomingDocumentController : Controller
    {
        private readonly IDocumentService _service;

        public IncomingDocumentController(IDocumentService service)
        {
            _service = service;
        }

        // ===================== INDEX =====================
        public async Task<IActionResult> Index(int page = 1)
        {
            var (documents, totalItems) =
                await _service.GetIncomingListAsync(page, 20);


            ViewBag.CurrentPage = page;
            ViewBag.TotalIncoming = totalItems;
            return View("~/Views/IncomingDocument/Index.cshtml", documents);
        }

        // ===================== CREATE (GET) =====================
        public async Task<IActionResult> Create()
        {
            await _service.LoadDropdownsAsync(
                s => ViewBag.Institutions = s,
                s => ViewBag.Classifications = s,
                s => ViewBag.AccessUsers = s,
                isEmployee: false
            );

            var model = new IncomingDocument
            {
                Priority = Priority.Normal,
                Classification = Classification.Public,
                ReceivedDate = DateTime.Now
            };

            return View("~/Views/IncomingDocument/Create.cshtml", model);
        }

        // ===================== CREATE (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            IncomingDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // business rule (optional)
            if (model.Classification == Classification.Secret)
            {
                ModelState.AddModelError(nameof(model.Classification),
                    "Nuk lejohet regjistrimi i dokumenteve sekrete.");
            }

            if (!ModelState.IsValid)
            {
                await _service.LoadDropdownsAsync(
                    s => ViewBag.Institutions = s,
                    s => ViewBag.Classifications = s,
                    s => ViewBag.AccessUsers = s,
                    isEmployee: false
                );

                return View("~/Views/IncomingDocument/Create.cshtml", model);
            }

            await _service.CreateIncomingAsync(
                model,
                attachmentFile,
                accessUserIds,
                scanSessionKey,
                userId
            );

            return RedirectToAction(nameof(Index));
        }

        // ===================== DETAILS =====================
        public async Task<IActionResult> Details(int id)
        {
            var document = await _service.GetIncomingByIdAsync(id);

            if (document == null)
                return NotFound();

            return View("~/Views/IncomingDocument/Details.cshtml", document);
        }
    }
}