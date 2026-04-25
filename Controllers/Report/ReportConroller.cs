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

            var vm = new ReportDashboardViewModel
            {
                TotalDocuments = await _service.GetTotalDocumentsAsync(role),
                TotalIncomingDocuments = await _service.GetTotalByTypeAsync(DocumentType.Incoming, role),
                TotalOutgoingDocuments = await _service.GetTotalByTypeAsync(DocumentType.Outgoing, role),
                TotalInternalDocuments = await _service.GetTotalByTypeAsync(DocumentType.Internal, role),

                TodayDocuments = await _service.GetTodayAsync(role),
                CurrentWeekDocuments = await _service.GetWeekAsync(role),
                CurrentMonthDocuments = await _service.GetMonthAsync(role),

                HighPriority = await _service.GetTotalByPriorityAsync(Priority.High, role),
                NormalPriority = await _service.GetTotalByPriorityAsync(Priority.Normal, role),
                LowPriority = await _service.GetTotalByPriorityAsync(Priority.Low, role),

                TopUsers = role != "Employee"
                    ? await _service.GetTopUsersAsync(role)
                    : new(),

                TopInstitutions = role != "Employee"
                    ? await _service.GetTopInstitutionsAsync(role)
                    : new()
            };

            return View(vm);
        }

        private string GetRole()
        {
            if (User.IsInRole("Administrator")) return "Administrator";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }
    }
}