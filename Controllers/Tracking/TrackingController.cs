using eProtokoll.Models;
using eProtokoll.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public class TrackingController : Controller
    {
        private readonly ITrackingRepository _trackingRepository;
        public TrackingController(
            ITrackingRepository trackingRepository)
        {
            _trackingRepository = trackingRepository;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            const int pageSize = 20;

            var userId = GetUserId();
            var role = GetRole();

            // 🔥 scope logic këtu
            var (trackings, totalCount) =
                role == "Employee"
                    ? await _trackingRepository.GetByUserAsync(page, pageSize, userId)
                    : await _trackingRepository.GetAllAsync(page, pageSize, searchTerm);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalItems = totalCount;

            return View(trackings);
        }

        // ================= ASSIGN (vetëm Manager/Admin) =================
        [HttpGet]
        public async Task<IActionResult> Assign(int? documentId)
        {
            if (!User.IsInRole("Manager") && !User.IsInRole("Administrator"))
                return Forbid();

            var tracking = new DocumentTracking
            {
                AssignedDate = DateTime.Now,
                Priority = Priority.Normal,
                DocumentId = documentId ?? 0
            };

            if (documentId.HasValue)
            {
                var documents = await _trackingRepository.GetDocumentsForDropdownAsync();
                ViewBag.Document = documents.FirstOrDefault(d => d.DocumentId == documentId.Value);
            }

            await LoadDropdowns(documentId);

            return View(tracking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(DocumentTracking model)
        {
            if (!User.IsInRole("Manager") && !User.IsInRole("Administrator"))
                return Forbid();

            var userId = GetUserId();

            await _trackingRepository.InsertAsync(model, userId);

            TempData["SuccessMessage"] = "Dokumenti u caktua me sukses!";
            return RedirectToAction(nameof(Index));
        }

        // ================= RESPOND (Employee) =================
        [HttpGet]
        public async Task<IActionResult> Respond(int id)
        {
            var userId = GetUserId();
            var tracking = await _trackingRepository.GetByIdAsync(id);

            if (tracking is null)
                return NotFound();

            if (tracking.AssignedToUserId != userId || tracking.CompletedDate.HasValue)
                return Forbid();

            return tracking.DocumentType switch
            {
                DocumentType.Incoming => RedirectToAction(
                    "Create",
                    "OutgoingDocument",
                    new { trackingId = id, originalIncomingDocumentId = tracking.DocumentId }),

                DocumentType.Internal => RedirectToAction(
                    "Create",
                    "InternalDocument",
                    new { trackingId = id }),

                _ => Forbid()
            };
        }

        // ================= COMPLETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = GetUserId();

            await _trackingRepository.CompleteAsync(id);

            TempData["SuccessMessage"] = "Dokumenti u shënua si i përfunduar.";
            return RedirectToAction(nameof(Index));
        }

        // ================= DROPDOWNS =================
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

        // ================= HELPERS =================
        private int GetUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetRole()
        {
            if (User.IsInRole("Administrator")) return "Administrator";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }
    }
}