using eProtokoll.Models;
using eProtokoll.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class ReportController : Controller
    {
        private readonly IReportRepository _reportRepository;

        public ReportController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        // GET: Admin/Report
        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportDashboardViewModel
            {
                // Dokumentet
                TotalDocuments = await _reportRepository.GetTotalDocumentsAsync(),
                TotalIncomingDocuments = await _reportRepository.GetTotalByDiscriminatorAsync("IncomingDocument"),
                TotalOutgoingDocuments = await _reportRepository.GetTotalByDiscriminatorAsync("OutgoingDocument"),
                TotalInternalDocuments = await _reportRepository.GetTotalByDiscriminatorAsync("InternalDocument"),

                // Institucionet
                TotalInstitutions = await _reportRepository.GetTotalInstitutionsAsync(),
                ActiveInstitutions = await _reportRepository.GetActiveInstitutionsAsync(),

                // Prioriteti
                LowPriorityDocuments = await _reportRepository.GetTotalByPriorityAsync(Priority.Low),
                NormalPriorityDocuments = await _reportRepository.GetTotalByPriorityAsync(Priority.Normal),
                HighPriorityDocuments = await _reportRepository.GetTotalByPriorityAsync(Priority.High),

                // Kohore
                CurrentMonthDocuments = await _reportRepository.GetCurrentMonthDocumentsAsync(),
                CurrentWeekDocuments = await _reportRepository.GetCurrentWeekDocumentsAsync(),
                TodayDocuments = await _reportRepository.GetTodayDocumentsAsync(),

                // Tracking
                ActiveTrackings = await _reportRepository.GetActiveTrackingsAsync(),
                CompletedTrackings = await _reportRepository.GetCompletedTrackingsAsync()
            };

            return View(viewModel);
        }

        // GET: Admin/Report/AuditLog
        public async Task<IActionResult> AuditLog()
        {
            var auditLogs = await _reportRepository.GetAuditLogAsync();
            return View(auditLogs);
        }
    }

    // ViewModel
    public class ReportDashboardViewModel
    {
        // Dokumentet
        public int TotalDocuments { get; set; }
        public int TotalIncomingDocuments { get; set; }
        public int TotalOutgoingDocuments { get; set; }
        public int TotalInternalDocuments { get; set; }

        // Institucionet
        public int TotalInstitutions { get; set; }
        public int ActiveInstitutions { get; set; }

        // Prioriteti
        public int LowPriorityDocuments { get; set; }
        public int NormalPriorityDocuments { get; set; }
        public int HighPriorityDocuments { get; set; }

        // Kohore
        public int CurrentMonthDocuments { get; set; }
        public int CurrentWeekDocuments { get; set; }
        public int TodayDocuments { get; set; }

        // Tracking
        public int ActiveTrackings { get; set; }
        public int CompletedTrackings { get; set; }
    }
}