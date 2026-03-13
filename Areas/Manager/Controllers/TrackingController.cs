using eProtokoll.Models;
using eProtokoll.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class TrackingController : Controller
    {
        private readonly ITrackingRepository _trackingRepository;

        public TrackingController(ITrackingRepository trackingRepository)
        {
            _trackingRepository = trackingRepository;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            const int pageSize = 20;

            var (trackings, totalCount) = await _trackingRepository.GetAllAsync(page, pageSize, searchTerm);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalItems = totalCount;

            return View(trackings);
        }

        // ================= ASSIGN GET =================
        public async Task<IActionResult> Assign(int? documentId)
        {
            var tracking = new DocumentTracking
            {
                AssignedDate = DateTime.Now,
                Priority = Priority.Normal,
                IsActive = true
            };

            if (documentId.HasValue)
            {
                tracking.DocumentId = documentId.Value;

                var documents = await _trackingRepository.GetDocumentsForDropdownAsync();
                var document = documents.FirstOrDefault(d => d.DocumentId == documentId.Value);
                if (document != null)
                    ViewBag.Document = document;
            }

            await LoadDropdowns(documentId);

            return View(tracking);
        }

        // ================= ASSIGN POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(DocumentTracking model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _trackingRepository.InsertAsync(model, userId);

            TempData["SuccessMessage"] = "Dokumenti u caktua me sukses!";
            return RedirectToAction(nameof(Index));
        }

        // ================= COMPLETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            await _trackingRepository.CompleteAsync(id);

            TempData["SuccessMessage"] = "Dokumenti u shënua si i përfunduar.";
            return RedirectToAction(nameof(Index));
        }

        // ================= CANCEL =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            await _trackingRepository.CancelAsync(id, reason);

            TempData["SuccessMessage"] = "Caktimi u anulua.";
            return RedirectToAction(nameof(Index));
        }

        // ================= LOAD DROPDOWNS =================
        private async Task LoadDropdowns(int? selectedDocumentId = null)
        {
            var documents = await _trackingRepository.GetDocumentsForDropdownAsync();
            var employees = await _trackingRepository.GetEmployeesAsync();

            ViewBag.Documents = new SelectList(
                documents.Select(d => new
                {
                    d.DocumentId,
                    DisplayText = $"{d.ProtocolNumber} - {d.Subject}"
                }),
                "DocumentId", "DisplayText", selectedDocumentId);

            ViewBag.Employees = new SelectList(
                employees.Select(e => new
                {
                    e.Id,
                    DisplayText = $"{e.FirstName} {e.LastName}" +
                                  (string.IsNullOrEmpty(e.Department) ? "" : $" ({e.Department})")
                }),
                "Id", "DisplayText");
        }
    }
}