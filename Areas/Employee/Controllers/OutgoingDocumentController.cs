using eProtokoll.Models;
using eProtokoll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class OutgoingDocumentController : Controller
    {
        private readonly IDocumentService _service;

        public OutgoingDocumentController(IDocumentService service)
        {
            _service = service;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1)
        {
            var (documents, totalItems) =
                await _service.GetOutgoingListAsync(page, 20);

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;

            return View("~/Views/OutgoingDocument/Index.cshtml", documents);
        }

        // ================= CREATE (GET) =================
        public async Task<IActionResult> Create()
        {
            await _service.LoadDropdownsAsync(
                setInstitutions: s => ViewBag.Institutions = s,
                setClassifications: s => ViewBag.Classifications = s,
                setUsers: s => ViewBag.AccessUsers = s,
                isEmployee: true
            );

            var model = new OutgoingDocument
            {
                Priority = Priority.Normal
            };

            return View("~/Views/OutgoingDocument/Create.cshtml", model);
        }

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            OutgoingDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // rule: employee cannot create secret documents
            if (model.Classification == Classification.Secret)
            {
                ModelState.AddModelError(
                    nameof(model.Classification),
                    "Nuk lejohet krijimi i dokumenteve sekrete."
                );
            }

            if (!ModelState.IsValid)
            {
                await _service.LoadDropdownsAsync(
                    setInstitutions: s => ViewBag.Institutions = s,
                    setClassifications: s => ViewBag.Classifications = s,
                    setUsers: s => ViewBag.AccessUsers = s,
                    isEmployee: true
                );

                return View("~/Views/OutgoingDocument/Create.cshtml", model);
            }

            await _service.CreateOutgoingAsync(
                model,
                attachmentFile,
                accessUserIds,
                scanSessionKey,
                userId
            );

            return RedirectToAction(nameof(Index));
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(int id)
        {
            var document = await _service.GetOutgoingByIdAsync(id);

            if (document == null)
                return NotFound();

            return View("~/Views/OutgoingDocument/Details.cshtml", document);
        }
    }
}