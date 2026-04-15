using eProtokoll.Models;
using eProtokoll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class InternalDocumentController : Controller
    {
        private readonly IDocumentService _service;

        public InternalDocumentController(IDocumentService service)
        {
            _service = service;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1)
        {
            var (documents, totalItems) =
                await _service.GetInternalListAsync(page, 20);

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;

            return View("~/Views/InternalDocument/Index.cshtml", documents);
        }

        // ================= CREATE (GET) =================
        public async Task<IActionResult> Create()
        {
            await _service.LoadDropdownsAsync(
                setInstitutions: s => ViewBag.Institutions = s,
                setClassifications: s => ViewBag.Classifications = s,
                setUsers: s => ViewBag.AccessUsers = s,
                isEmployee: false
            );

            var model = new InternalDocument
            {
                Priority = Priority.Normal
            };

            return View("~/Views/InternalDocument/Create.cshtml", model);
        }

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            InternalDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _service.CreateInternalAsync(
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
            var document = await _service.GetInternalByIdAsync(id);

            if (document == null)
                return NotFound();

            return View("~/Views/InternalDocument/Details.cshtml", document);
        }
    }
}