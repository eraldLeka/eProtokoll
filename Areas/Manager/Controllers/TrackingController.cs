using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class TrackingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Manager/Tracking
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string assignedTo = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var query = _context.DocumentTrackings
                .Include(t => t.Document)
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedByUser)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t =>
                    t.Instructions.Contains(searchTerm) ||
                    (t.Document != null && t.Document.ProtocolNumber.Contains(searchTerm)) ||
                    (t.Document != null && t.Document.Subject.Contains(searchTerm)));
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TrackingStatus>(status, out var trackStatus))
            {
                query = query.Where(t => t.Status == trackStatus);
            }

            // Filter by priority
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var trackPriority))
            {
                query = query.Where(t => t.Priority == trackPriority);
            }

            // Filter by assigned user
            if (!string.IsNullOrEmpty(assignedTo))
            {
                query = query.Where(t => t.AssignedToUserId == assignedTo);
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                query = query.Where(t => t.AssignedDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(t => t.AssignedDate <= dateTo.Value);
            }

            // Total count and paging
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var trackings = await query
                .OrderByDescending(t => t.AssignedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag for filters
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPriority = priority;
            ViewBag.SelectedAssignedTo = assignedTo;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Load users for filter dropdown
            var users = await _context.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "FullName");

            // Statistics
            ViewBag.TotalTrackings = await _context.DocumentTrackings.CountAsync();

            var today = DateTime.Now.Date;
            ViewBag.TodayAssigned = await _context.DocumentTrackings
                .Where(t => t.AssignedDate.Date == today)
                .CountAsync();

            ViewBag.Pending = await _context.DocumentTrackings
                .Where(t => t.Status == TrackingStatus.Assigned || t.Status == TrackingStatus.Accepted)
                .CountAsync();

            ViewBag.InProgress = await _context.DocumentTrackings
                .Where(t => t.Status == TrackingStatus.InProgress)
                .CountAsync();

            ViewBag.Completed = await _context.DocumentTrackings
                .Where(t => t.Status == TrackingStatus.Completed)
                .CountAsync();

            ViewBag.Overdue = await _context.DocumentTrackings
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && !t.IsCompleted)
                .CountAsync();

            return View(trackings);
        }

        // GET: Manager/Tracking/Create
        public async Task<IActionResult> Create(int? documentId)
        {
            var tracking = new DocumentTracking
            {
                AssignedDate = DateTime.Now,
                AssignedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
                Priority = Priority.Normal,
                Status = TrackingStatus.Assigned,
                ActionType = ActionType.ForAction,
                IsActive = true
            };

            if (documentId.HasValue)
            {
                var document = await _context.Documents
                    .Include(d => d.Classification)
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId.Value);

                if (document != null)
                {
                    tracking.DocumentId = documentId.Value;
                    ViewBag.Document = document;
                }
            }

            await LoadDropdowns();
            return View(tracking);
        }

        // POST: Manager/Tracking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentTracking model)
        {
            if (ModelState.IsValid)
            {
                // Set metadata
                model.AssignedByUserId = User.Identity.Name;
                model.IsActive = true;

                // Calculate sequence number for this document
                var maxSequence = await _context.DocumentTrackings
                    .Where(t => t.DocumentId == model.DocumentId)
                    .MaxAsync(t => (int?)t.SequenceNumber) ?? 0;
                model.SequenceNumber = maxSequence + 1;

                _context.DocumentTrackings.Add(model);
                await _context.SaveChangesAsync();

                // Update document status
                var document = await _context.Documents.FindAsync(model.DocumentId);
                if (document != null && document.Status == DocumentStatus.Draft)
                {
                    document.Status = DocumentStatus.InProgress;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Dokumenti u caktua me sukses!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(model);
        }

        // GET: Manager/Tracking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var tracking = await _context.DocumentTrackings
                .Include(t => t.Document)
                    .ThenInclude(d => d.Classification)
                .Include(t => t.Document)
                    .ThenInclude(d => d.Attachments)
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedByUser)
                .Include(t => t.DelegatedToTracking)
                    .ThenInclude(dt => dt.AssignedToUser)
                .Include(t => t.ParentTracking)
                .Include(t => t.SubDelegations)
                    .ThenInclude(sd => sd.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TrackingId == id);

            if (tracking == null) return NotFound();

            return View(tracking);
        }

        // POST: Manager/Tracking/Accept/5
        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            var tracking = await _context.DocumentTrackings.FindAsync(id);
            if (tracking == null)
                return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });

            tracking.IsAccepted = true;
            tracking.AcceptedDate = DateTime.Now;
            tracking.Status = TrackingStatus.Accepted;
            tracking.ModifiedDate = DateTime.Now;
            tracking.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gjurmimi u pranua me sukses!" });
        }

        // POST: Manager/Tracking/Start/5
        [HttpPost]
        public async Task<IActionResult> Start(int id)
        {
            var tracking = await _context.DocumentTrackings.FindAsync(id);
            if (tracking == null)
                return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });

            tracking.IsInProgress = true;
            tracking.StartedDate = DateTime.Now;
            tracking.Status = TrackingStatus.InProgress;
            tracking.ModifiedDate = DateTime.Now;
            tracking.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gjurmimi u nis me sukses!" });
        }

        // POST: Manager/Tracking/Complete/5
        [HttpPost]
        public async Task<IActionResult> Complete(int id, string comment, int percentage)
        {
            var tracking = await _context.DocumentTrackings.FindAsync(id);
            if (tracking == null)
                return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });

            tracking.IsCompleted = true;
            tracking.CompletedDate = DateTime.Now;
            tracking.CompletionComment = comment;
            tracking.CompletionPercentage = percentage;
            tracking.Status = TrackingStatus.Completed;
            tracking.ModifiedDate = DateTime.Now;
            tracking.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gjurmimi u përfundua me sukses!" });
        }

        // GET: Manager/Tracking/Delegate/5
        public async Task<IActionResult> Delegate(int? id)
        {
            if (id == null) return NotFound();

            var parentTracking = await _context.DocumentTrackings
                .Include(t => t.Document)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TrackingId == id);

            if (parentTracking == null) return NotFound();

            var newTracking = new DocumentTracking
            {
                DocumentId = parentTracking.DocumentId,
                ParentTrackingId = parentTracking.TrackingId,
                AssignedDate = DateTime.Now,
                AssignedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
                Priority = parentTracking.Priority,
                Status = TrackingStatus.Assigned,
                ActionType = parentTracking.ActionType,
                IsActive = true
            };

            ViewBag.ParentTracking = parentTracking;
            await LoadDropdowns();
            return View(newTracking);
        }

        // POST: Manager/Tracking/Delegate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delegate(DocumentTracking model)
        {
            if (ModelState.IsValid)
            {
                // Set metadata
                model.AssignedByUserId = User.Identity.Name;
                model.IsActive = true;

                // Calculate sequence number
                var maxSequence = await _context.DocumentTrackings
                    .Where(t => t.DocumentId == model.DocumentId)
                    .MaxAsync(t => (int?)t.SequenceNumber) ?? 0;
                model.SequenceNumber = maxSequence + 1;

                _context.DocumentTrackings.Add(model);

                // Update parent tracking
                if (model.ParentTrackingId.HasValue)
                {
                    var parentTracking = await _context.DocumentTrackings.FindAsync(model.ParentTrackingId.Value);
                    if (parentTracking != null)
                    {
                        parentTracking.IsDelegated = true;
                        parentTracking.DelegatedToTrackingId = model.TrackingId;
                        parentTracking.Status = TrackingStatus.Delegated;
                        parentTracking.ModifiedDate = DateTime.Now;
                        parentTracking.ModifiedBy = User.Identity?.Name ?? "System";
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Dokumenti u delegua me sukses!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(model);
        }

        // POST: Manager/Tracking/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var tracking = await _context.DocumentTrackings.FindAsync(id);
            if (tracking == null)
                return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });

            tracking.Status = TrackingStatus.Cancelled;
            tracking.IsActive = false;
            tracking.Notes = $"Anulluar: {reason}";
            tracking.ModifiedDate = DateTime.Now;
            tracking.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gjurmimi u anullua me sukses!" });
        }

        // POST: Manager/Tracking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tracking = await _context.DocumentTrackings.FindAsync(id);

            if (tracking == null)
            {
                TempData["ErrorMessage"] = "Gjurmimi nuk u gjet!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.DocumentTrackings.Remove(tracking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gjurmimi u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========== HELPER METHODS ==========

        private async Task LoadDropdowns(int? selectedDocumentId = null, string? selectedUserId = null)
        {
            // Documents dropdown
            var documents = await _context.Documents
                .OrderByDescending(d => d.CreatedDate)
                .Take(100) // Limit to recent 100 documents
                .ToListAsync();

            ViewBag.Documents = new SelectList(
                documents.Select(d => new
                {
                    d.DocumentId,
                    DisplayText = $"{d.ProtocolNumber} - {d.Subject}"
                }),
                "DocumentId",
                "DisplayText",
                selectedDocumentId
            );

            // Users dropdown
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "FullName", selectedUserId);
        }

        private async Task<bool> TrackingExists(int id)
        {
            return await _context.DocumentTrackings.AnyAsync(t => t.TrackingId == id);
        }
    }
}