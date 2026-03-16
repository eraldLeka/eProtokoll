using eProtokoll.Repositories.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class DashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _dashboardRepository.GetAdminStatsAsync();
            var recentActivity = await _dashboardRepository.GetRecentActivityAsync();
            var monthlyData = await _dashboardRepository.GetMonthlyDataAsync();

            ViewBag.TotalUsers = stats.TotalUsers;
            ViewBag.TotalDocuments = stats.TotalDocuments;
            ViewBag.DocumentsToday = stats.DocumentsToday;
            ViewBag.TotalInstitutions = stats.TotalInstitutions;
            ViewBag.UsersThisMonth = stats.UsersThisMonth;
            ViewBag.InstitutionsThisMonth = stats.InstitutionsThisMonth;
            ViewBag.Incoming = stats.Incoming;
            ViewBag.Outgoing = stats.Outgoing;
            ViewBag.Internal = stats.Internal;
            ViewBag.Total = stats.Total;
            ViewBag.IncomingPct = stats.IncomingPct;
            ViewBag.OutgoingPct = stats.OutgoingPct;
            ViewBag.InternalPct = stats.InternalPct;
            ViewBag.RecentActivity = recentActivity;
            ViewBag.MonthNames = monthlyData.Select(m => m.MonthName).ToList();
            ViewBag.MonthlyCounts = monthlyData.Select(m => m.Count).ToList();

            return View();
        }
    }
}