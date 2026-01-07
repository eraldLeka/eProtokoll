using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class ProtocolBookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProtocolBookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Manager/ProtocolBook
        public async Task<IActionResult> Index(string searchTerm = "", string documentType = "",
            string status = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 50;
            var query = _context.Documents
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d =>
                    d.ProtocolNumber.Contains(searchTerm) ||
                    d.Subject.Contains(searchTerm) ||
                    (d.Content != null && d.Content.Contains(searchTerm)));
            }

            // Filter by document type
            if (!string.IsNullOrEmpty(documentType) && Enum.TryParse<DocumentType>(documentType, out var docType))
            {
                query = query.Where(d => d.DocumentType == docType);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var docStatus))
            {
                query = query.Where(d => d.Status == docStatus);
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                query = query.Where(d => d.ProtocolDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(d => d.ProtocolDate <= dateTo.Value);
            }

            // Total count and paging
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var documents = await query
                .OrderByDescending(d => d.ProtocolDate)
                .ThenByDescending(d => d.ProtocolTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag for filters
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedDocumentType = documentType;
            ViewBag.SelectedStatus = status;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Statistics
            var today = DateTime.Now.Date;
            ViewBag.TotalDocuments = await _context.Documents.CountAsync();
            ViewBag.TodayDocuments = await _context.Documents.Where(d => d.ProtocolDate == today).CountAsync();
            ViewBag.IncomingCount = await _context.IncomingDocuments.CountAsync();
            ViewBag.OutgoingCount = await _context.OutgoingDocuments.CountAsync();
            ViewBag.InternalCount = await _context.InternalDocuments.CountAsync();

            return View(documents);
        }

        // GET: Manager/ProtocolBook/Details/5
        public async Task<IActionResult> Details(int? id, string type)
        {
            if (id == null || string.IsNullOrEmpty(type)) return NotFound();

            // Redirect to appropriate controller based on document type
            return type.ToLower() switch
            {
                "incoming" => RedirectToAction("Details", "IncomingDocument", new { id }),
                "outgoing" => RedirectToAction("Details", "OutgoingDocument", new { id }),
                "internal" => RedirectToAction("Details", "InternalDocument", new { id }),
                _ => NotFound()
            };
        }
    }
}