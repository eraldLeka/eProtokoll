using eProtokoll.Repositories.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class DashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var stats = await _dashboardRepository.GetDocumentStatsAsync(userId);
            var recentDocuments = await _dashboardRepository.GetRecentDocumentsAsync(userId);
            var dailyStats = await _dashboardRepository.GetDailyStatsAsync(7, userId);

            ViewBag.TotalIncoming = stats.TotalIncoming;
            ViewBag.TotalOutgoing = stats.TotalOutgoing;
            ViewBag.TotalInternal = stats.TotalInternal;
            ViewBag.TotalDocuments = stats.TotalDocuments;
            ViewBag.RecentDocuments = recentDocuments;
            ViewBag.DailyStats = dailyStats;

            return View();
        }
    }
}