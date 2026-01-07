using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Manager/Report
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            // Total counts
            ViewBag.TotalDocuments = await _context.Documents.CountAsync();
            ViewBag.TotalIncoming = await _context.IncomingDocuments.CountAsync();
            ViewBag.TotalOutgoing = await _context.OutgoingDocuments.CountAsync();
            ViewBag.TotalInternal = await _context.InternalDocuments.CountAsync();

            // By period
            ViewBag.TodayCount = await _context.Documents.Where(d => d.CreatedDate.Date == today).CountAsync();
            ViewBag.MonthCount = await _context.Documents.Where(d => d.CreatedDate >= thisMonth).CountAsync();
            ViewBag.YearCount = await _context.Documents.Where(d => d.CreatedDate >= thisYear).CountAsync();

            // By status
            ViewBag.Draft = await _context.Documents.Where(d => d.Status == DocumentStatus.Draft).CountAsync();
            ViewBag.InProgress = await _context.Documents.Where(d => d.Status == DocumentStatus.InProgress).CountAsync();
            ViewBag.Completed = await _context.Documents.Where(d => d.Status == DocumentStatus.Completed).CountAsync();
            ViewBag.Archived = await _context.Documents.Where(d => d.IsArchived).CountAsync();

            // By priority
            ViewBag.Urgent = await _context.Documents.Where(d => d.Priority == Priority.Urgent).CountAsync();
            ViewBag.High = await _context.Documents.Where(d => d.Priority == Priority.High).CountAsync();
            ViewBag.Normal = await _context.Documents.Where(d => d.Priority == Priority.Normal).CountAsync();
            ViewBag.Low = await _context.Documents.Where(d => d.Priority == Priority.Low).CountAsync();

            // Monthly data for chart (last 12 months)
            var monthlyData = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = today.AddMonths(-i).Date;
                monthStart = new DateTime(monthStart.Year, monthStart.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var count = await _context.Documents
                    .Where(d => d.CreatedDate >= monthStart && d.CreatedDate < monthEnd)
                    .CountAsync();

                monthlyData.Add(new
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Count = count
                });
            }
            ViewBag.MonthlyData = monthlyData;

            // Top institutions
            var topInstitutions = await _context.IncomingDocuments
                .Include(d => d.Institution)
                .Where(d => d.CreatedDate >= thisYear)
                .GroupBy(d => d.InstitutionId)
                .Select(g => new
                {
                    InstitutionId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var topInstList = new List<object>();
            foreach (var item in topInstitutions)
            {
                var inst = await _context.Institutions.FindAsync(item.InstitutionId);
                topInstList.Add(new { Name = inst?.Name ?? "N/A", Count = item.Count });
            }
            ViewBag.TopInstitutions = topInstList;

            // Top users
            var topUsers = await _context.Documents
                .Where(d => d.CreatedDate >= thisYear)
                .GroupBy(d => d.CreatedBy)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var topUsersList = new List<object>();
            foreach (var item in topUsers)
            {
                var user = await _context.Users.FindAsync(item.UserId);
                topUsersList.Add(new { Name = user?.FullName ?? "N/A", Count = item.Count });
            }
            ViewBag.TopUsers = topUsersList;

            return View();
        }
    }
}