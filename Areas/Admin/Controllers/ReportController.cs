using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Report
        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportDashboardViewModel
            {
                // Statistika të përgjithshme
                TotalDocuments = await _context.Documents.CountAsync(),
                TotalIncomingDocuments = await _context.IncomingDocuments.CountAsync(),
                TotalOutgoingDocuments = await _context.OutgoingDocuments.CountAsync(),
                TotalInternalDocuments = await _context.InternalDocuments.CountAsync(),

                TotalInstitutions = await _context.Institutions.CountAsync(),
                ActiveInstitutions = await _context.Institutions.CountAsync(i => i.IsActive),

                TotalClassifications = await _context.Classifications.CountAsync(),

                // Dokumente sipas statusit
                DraftDocuments = await _context.Documents.CountAsync(d => d.Status == DocumentStatus.Draft),
                RegisteredDocuments = await _context.Documents.CountAsync(d => d.Status == DocumentStatus.Registered),
                InProgressDocuments = await _context.Documents.CountAsync(d => d.Status == DocumentStatus.InProgress),
                CompletedDocuments = await _context.Documents.CountAsync(d => d.Status == DocumentStatus.Completed),

                // Dokumente sipas prioritetit
                LowPriorityDocuments = await _context.Documents.CountAsync(d => d.Priority == Priority.Low),
                NormalPriorityDocuments = await _context.Documents.CountAsync(d => d.Priority == Priority.Normal),
                HighPriorityDocuments = await _context.Documents.CountAsync(d => d.Priority == Priority.High),
                UrgentPriorityDocuments = await _context.Documents.CountAsync(d => d.Priority == Priority.Urgent),

                // Dokumente të muajit aktual
                CurrentMonthDocuments = await _context.Documents
                    .CountAsync(d => d.CreatedDate.Month == DateTime.Now.Month &&
                                     d.CreatedDate.Year == DateTime.Now.Year),

                // Dokumente të javës aktuale
                CurrentWeekDocuments = await _context.Documents
                    .CountAsync(d => d.CreatedDate >= DateTime.Now.AddDays(-7)),

                // Dokumente të sotme
                TodayDocuments = await _context.Documents
                    .CountAsync(d => d.CreatedDate.Date == DateTime.Now.Date)
            };

            return View(viewModel);
        }

        // GET: Admin/Report/DocumentsByType
        public async Task<IActionResult> DocumentsByType(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var documents = await _context.Documents
                .Where(d => d.CreatedDate >= startDate && d.CreatedDate <= endDate)
                .GroupBy(d => d.DocumentType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(documents);
        }

        // GET: Admin/Report/DocumentsByStatus
        public async Task<IActionResult> DocumentsByStatus()
        {
            var documents = await _context.Documents
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return View(documents);
        }

        // GET: Admin/Report/DocumentsByInstitution
        public async Task<IActionResult> DocumentsByInstitution()
        {
            var incoming = await _context.IncomingDocuments
                .Include(d => d.Institution)
                .GroupBy(d => d.Institution.Name)
                .Select(g => new { Institution = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var outgoing = await _context.OutgoingDocuments
                .Include(d => d.Institution)
                .GroupBy(d => d.Institution.Name)
                .Select(g => new { Institution = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewBag.IncomingDocuments = incoming;
            ViewBag.OutgoingDocuments = outgoing;

            return View();
        }

        // GET: Admin/Report/MonthlyReport
        public async Task<IActionResult> MonthlyReport(int? year, int? month)
        {
            year ??= DateTime.Now.Year;
            month ??= DateTime.Now.Month;

            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var documents = await _context.Documents
                .Where(d => d.CreatedDate >= startDate && d.CreatedDate <= endDate)
                .ToListAsync();

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.MonthName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");

            return View(documents);
        }

        // GET: Admin/Report/ExportData
        public IActionResult ExportData()
        {
            return View();
        }

        // GET: Admin/Report/AuditLog
        public async Task<IActionResult> AuditLog(DateTime? startDate, DateTime? endDate, string userId = null, string actionType = null)
        {
            startDate ??= DateTime.Now.AddDays(-30);
            endDate ??= DateTime.Now;

            var query = _context.DocumentTrackings
                .Include(t => t.Document)
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedByUser)
                .Where(t => t.CreatedDate >= startDate && t.CreatedDate <= endDate);

            // Filtro sipas përdoruesit
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(t => t.AssignedToUserId == userId || t.AssignedByUserId == userId);
            }

            // Filtro sipas llojit të veprimit
            if (!string.IsNullOrEmpty(actionType) && Enum.TryParse<ActionType>(actionType, out var action))
            {
                query = query.Where(t => t.ActionType == action);
            }

            var auditLogs = await query
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedActionType = actionType;

            // Lista e përdoruesve për dropdown
            ViewBag.Users = await _context.Users
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            return View(auditLogs);
        }
    }

    // ViewModel për Dashboard
    public class ReportDashboardViewModel
    {
        public int TotalDocuments { get; set; }
        public int TotalIncomingDocuments { get; set; }
        public int TotalOutgoingDocuments { get; set; }
        public int TotalInternalDocuments { get; set; }

        public int TotalInstitutions { get; set; }
        public int ActiveInstitutions { get; set; }

        public int TotalClassifications { get; set; }

        public int DraftDocuments { get; set; }
        public int RegisteredDocuments { get; set; }
        public int InProgressDocuments { get; set; }
        public int CompletedDocuments { get; set; }

        public int LowPriorityDocuments { get; set; }
        public int NormalPriorityDocuments { get; set; }
        public int HighPriorityDocuments { get; set; }
        public int UrgentPriorityDocuments { get; set; }

        public int CurrentMonthDocuments { get; set; }
        public int CurrentWeekDocuments { get; set; }
        public int TodayDocuments { get; set; }
    }
}