using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

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
            var today = DateTime.Now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            // === STATISTIKA BAZË ===
            ViewBag.TotalDocuments = await _context.Documents.CountAsync();
            ViewBag.TotalIncoming = await _context.IncomingDocuments.CountAsync();
            ViewBag.TotalOutgoing = await _context.OutgoingDocuments.CountAsync();
            ViewBag.TotalInternal = await _context.InternalDocuments.CountAsync();

            ViewBag.TodayDocuments = await _context.Documents
                .Where(d => d.CreatedDate.Date == today)
                .CountAsync();

            ViewBag.MonthDocuments = await _context.Documents
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            // === PRIORITETET ===
            ViewBag.UrgentDocuments = await _context.Documents
                .Where(d => d.Priority == Priority.Urgent && d.Status != DocumentStatus.Completed)
                .CountAsync();

            // === STATUSET ===
            ViewBag.InProgressDocuments = await _context.Documents
                .Where(d => d.Status == DocumentStatus.InProgress)
                .CountAsync();

            ViewBag.CompletedDocuments = await _context.Documents
                .Where(d => d.Status == DocumentStatus.Completed)
                .CountAsync();

            // === DOKUMENTET E FUNDIT ===
            var recentDocuments = await _context.Documents
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .OrderByDescending(d => d.CreatedDate)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentDocuments = recentDocuments;

            // === AKTIVITETI I 7 DITËVE ===
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

            // === DOKUMENTET PËR MUAJIN ===
            ViewBag.MonthlyIncoming = await _context.IncomingDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            ViewBag.MonthlyOutgoing = await _context.OutgoingDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            ViewBag.MonthlyInternal = await _context.InternalDocuments
                .Where(d => d.CreatedDate >= thisMonth)
                .CountAsync();

            return View();
        }
    }
}