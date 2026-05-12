using eProtokoll.Models;
using eProtokoll.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public class ReportController : Controller
    {
        private readonly ReportService _service;

        public ReportController(ReportService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var role = GetRole();
            var userId = GetUserId();
            ViewData["role"] = role;

            var vm = new ReportDashboardViewModel
            {
                TotalDocuments = await _service.GetTotalDocumentsAsync(role, userId),
                TotalIncomingDocuments = await _service.GetTotalByTypeAsync(DocumentType.Incoming, role, userId),
                TotalOutgoingDocuments = await _service.GetTotalByTypeAsync(DocumentType.Outgoing, role, userId),
                TotalInternalDocuments = await _service.GetTotalByTypeAsync(DocumentType.Internal, role, userId),

                TodayDocuments = await _service.GetTodayAsync(role, userId),
                CurrentWeekDocuments = await _service.GetWeekAsync(role, userId),
                CurrentMonthDocuments = await _service.GetMonthAsync(role, userId),

                HighPriority = await _service.GetTotalByPriorityAsync(Priority.High, role, userId),
                NormalPriority = await _service.GetTotalByPriorityAsync(Priority.Normal, role, userId),
                LowPriority = await _service.GetTotalByPriorityAsync(Priority.Low, role, userId),

                TrackingActive = await _service.GetTrackingActiveAsync(role, userId),
                TrackingOverdue = await _service.GetTrackingOverdueAsync(role, userId),
                TrackingCompleted = await _service.GetTrackingCompletedAsync(role, userId),

                TopUsers = role != "Employee"
                    ? await _service.GetTopUsersAsync(role)
                    : new(),

                TopInstitutions = role != "Employee"
                    ? await _service.GetTopInstitutionsAsync(role)
                    : new()
            };

            return View(vm);
        }

        private int GetUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetRole()
        {
            if (User.IsInRole("Administrator")) return "Administrator";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }
    }
}