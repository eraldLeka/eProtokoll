using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;
using Microsoft.AspNetCore.Authorization;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // === STATISTIKA DOKUMENTESH ===
            var today = DateTime.Now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            // Total dokumentet
            ViewBag.TotalDocuments = await _context.Documents.CountAsync();
            ViewBag.TotalIncoming = await _context.IncomingDocuments.CountAsync();
            ViewBag.TotalOutgoing = await _context.OutgoingDocuments.CountAsync();
            ViewBag.TotalInternal = await _context.InternalDocuments.CountAsync();

            // Dokumentet e sotme
            ViewBag.TodayDocuments = await _context.Documents
                .Where(d => d.CreatedDate.Date == today)
                .CountAsync();

            // Dokumentet e muajit
            ViewBag.MonthDocuments = await _context.Documents
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            // Dokumentet e vitit
            ViewBag.YearDocuments = await _context.Documents
                .Where(d => d.CreatedDate >= thisYear)
                .CountAsync();

            // === STATUSET E DOKUMENTEVE ===
            ViewBag.DraftDocuments = await _context.Documents
                .Where(d => d.Status == DocumentStatus.Draft)
                .CountAsync();

            ViewBag.InProgressDocuments = await _context.Documents
                .Where(d => d.Status == DocumentStatus.InProgress)
                .CountAsync();

            ViewBag.PendingDocuments = await _context.Documents
                .Where(d => d.Status == DocumentStatus.Pending)
                .CountAsync();

            ViewBag.CompletedDocuments = await _context.Documents
                .Where(d => d.Status == DocumentStatus.Completed)
                .CountAsync();

            ViewBag.ArchivedDocuments = await _context.Documents
                .Where(d => d.IsArchived)
                .CountAsync();

            // === PRIORITETET ===
            ViewBag.UrgentDocuments = await _context.Documents
                .Where(d => d.Priority == Priority.Urgent && d.Status != DocumentStatus.Completed)
                .CountAsync();

            ViewBag.HighPriorityDocuments = await _context.Documents
                .Where(d => d.Priority == Priority.High && d.Status != DocumentStatus.Completed)
                .CountAsync();

            // === AFATET ===
            ViewBag.TotalDeadlines = await _context.Deadlines
                .Where(d => d.IsActive && !d.IsCompleted)
                .CountAsync();

            ViewBag.OverdueDeadlines = await _context.Deadlines
                .Where(d => d.IsActive && !d.IsCompleted && d.DueDate < today)
                .CountAsync();

            ViewBag.TodayDeadlines = await _context.Deadlines
                .Where(d => d.IsActive && !d.IsCompleted && d.DueDate.Date == today)
                .CountAsync();

            ViewBag.UpcomingDeadlines = await _context.Deadlines
                .Where(d => d.IsActive && !d.IsCompleted && d.DueDate > today && d.DueDate <= today.AddDays(7))
                .CountAsync();

            // === AFATET QË AFROJNË (7 ditët e ardhshme) ===
            var upcomingDeadlines = await _context.Deadlines
                .Include(d => d.Document)
                .Include(d => d.ResponsibleUser)
                .Where(d => d.IsActive &&
                           !d.IsCompleted &&
                           d.DueDate >= today &&
                           d.DueDate <= today.AddDays(7))
                .OrderBy(d => d.DueDate)
                .Take(10)
                .ToListAsync();

            ViewBag.UpcomingDeadlinesList = upcomingDeadlines;

            // === AFATET E VONUARA ===
            var overdueDeadlines = await _context.Deadlines
                .Include(d => d.Document)
                .Include(d => d.ResponsibleUser)
                .Where(d => d.IsActive && !d.IsCompleted && d.DueDate < today)
                .OrderBy(d => d.DueDate)
                .Take(10)
                .ToListAsync();

            ViewBag.OverdueDeadlinesList = overdueDeadlines;

            // === DOKUMENTET E FUNDIT (10 të fundit) ===
            var recentDocuments = await _context.Documents
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .OrderByDescending(d => d.CreatedDate)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentDocuments = recentDocuments;

            // === DOKUMENTET QË KËRKOJNË PËRGJIGJE ===
            var needsResponse = await _context.IncomingDocuments
                .Include(d => d.Institution)
                .Where(d => d.RequiresResponse && !d.IsResponded)
                .OrderBy(d => d.ResponseDeadline)
                .Take(10)
                .ToListAsync();

            ViewBag.NeedsResponseList = needsResponse;
            ViewBag.NeedsResponseCount = needsResponse.Count;

            // === DOKUMENTET E BRENDSHME QË KËRKOJNË MIRATIM ===
            var needsApproval = await _context.InternalDocuments
                .Include(d => d.FromUser)
                .Where(d => d.RequiresApproval && !d.IsApproved)
                .OrderByDescending(d => d.CreatedDate)
                .Take(10)
                .ToListAsync();

            ViewBag.NeedsApprovalList = needsApproval;
            ViewBag.NeedsApprovalCount = needsApproval.Count;

            // === AKTIVITETI I FUNDIT (për grafik) ===
            // Dokumentet e 7 ditëve të fundit (për Chart.js)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .Reverse()
                .ToList();

            var dailyStats = new List<object>();
            foreach (var day in last7Days)
            {
                var count = await _context.Documents
                    .Where(d => d.CreatedDate.Date == day.Date)
                    .CountAsync();

                dailyStats.Add(new
                {
                    Date = day.ToString("dd/MM"),
                    Count = count
                });
            }

            ViewBag.DailyStats = dailyStats;

            // === DOKUMENTET PËR MUAJIN (për grafik) ===
            var monthlyIncoming = await _context.IncomingDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            var monthlyOutgoing = await _context.OutgoingDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            var monthlyInternal = await _context.InternalDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            ViewBag.MonthlyIncoming = monthlyIncoming;
            ViewBag.MonthlyOutgoing = monthlyOutgoing;
            ViewBag.MonthlyInternal = monthlyInternal;

            // === PËRDORUESIT MË AKTIVË (Top 5) ===
            var topUsers = await _context.Documents
                .Where(d => d.CreatedDate >= thisMonth)
                .GroupBy(d => d.CreatedBy)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topUsersWithNames = new List<object>();
            foreach (var user in topUsers)
            {
                var appUser = await _context.Users.FindAsync(user.UserId);
                topUsersWithNames.Add(new
                {
                    Name = appUser?.FullName ?? "Unknown",
                    Count = user.Count
                });
            }

            ViewBag.TopUsers = topUsersWithNames;

            // === INSTITUCIONET MË AKTIVE (Top 5) ===
            var topInstitutions = await _context.IncomingDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .GroupBy(d => d.InstitutionId)
                .Select(g => new
                {
                    InstitutionId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topInstitutionsWithNames = new List<object>();
            foreach (var inst in topInstitutions)
            {
                var institution = await _context.Institutions.FindAsync(inst.InstitutionId);
                topInstitutionsWithNames.Add(new
                {
                    Name = institution?.Name ?? "Unknown",
                    Count = inst.Count
                });
            }

            ViewBag.TopInstitutions = topInstitutionsWithNames;

            return View();
        }

        // === QUICK STATS për AJAX (opsionale) ===
        [HttpGet]
        public async Task<IActionResult> GetQuickStats()
        {
            var today = DateTime.Now.Date;

            var stats = new
            {
                TotalDocuments = await _context.Documents.CountAsync(),
                TodayDocuments = await _context.Documents.Where(d => d.CreatedDate.Date == today).CountAsync(),
                UrgentDocuments = await _context.Documents.Where(d => d.Priority == Priority.Urgent && d.Status != DocumentStatus.Completed).CountAsync(),
                OverdueDeadlines = await _context.Deadlines.Where(d => d.IsActive && !d.IsCompleted && d.DueDate < today).CountAsync()
            };

            return Json(stats);
        }
    }
}