using eProtokoll.Repositories.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class DashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _dashboardRepository.GetDocumentStatsAsync();
            var recentDocuments = await _dashboardRepository.GetRecentDocumentsAsync();
            var dailyStats = await _dashboardRepository.GetDailyStatsAsync();

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