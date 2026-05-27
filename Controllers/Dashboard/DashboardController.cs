using eProtokoll.Repositories.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public class DashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index()
        {
            var role = GetRole();
            int? userId = role == "Employee"
                ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : null;

            ViewBag.RecentDocuments = await _dashboardRepository.GetRecentDocumentsAsync(userId);
            ViewBag.DailyStats = await _dashboardRepository.GetDailyStatsAsync(7, userId);
            ViewData["area"] = role;

            return View();
        }

        private string GetRole()
        {
            if (User.IsInRole("Administrator")) return "Administrator";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }
    }
}