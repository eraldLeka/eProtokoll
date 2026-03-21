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

        public async Task<IActionResult> Index(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            var monthlyData = await _reportRepository.GetMonthlyDocumentCountsAsync(selectedYear);
            int maxCount = monthlyData.Any() ? monthlyData.Max(m => m.Count) : 1;
            if (maxCount == 0) maxCount = 1;

            var viewModel = new ReportDashboardViewModel
            {
                TotalDocuments = await _reportRepository.GetTotalDocumentsAsync(),
                TotalIncomingDocuments = await _reportRepository.GetTotalByTypeAsync(DocumentType.Incoming),
                TotalOutgoingDocuments = await _reportRepository.GetTotalByTypeAsync(DocumentType.Outgoing),
                TotalInternalDocuments = await _reportRepository.GetTotalByTypeAsync(DocumentType.Internal),
                TotalInstitutions = await _reportRepository.GetTotalInstitutionsAsync(),
                CurrentMonthDocuments = await _reportRepository.GetCurrentMonthDocumentsAsync(),
                CurrentWeekDocuments = await _reportRepository.GetCurrentWeekDocumentsAsync(),
                TodayDocuments = await _reportRepository.GetTodayDocumentsAsync(),
                MonthlyData = monthlyData,
                MaxMonthlyCount = maxCount,
                SelectedYear = selectedYear,
                TopInstitutions = await _reportRepository.GetTopInstitutionsAsync(5),
                TopUsers = await _reportRepository.GetTopUsersAsync(5)
            };

            return View(viewModel);
        }
    }

    public class ReportDashboardViewModel
    {
        public int TotalDocuments { get; set; }
        public int TotalIncomingDocuments { get; set; }
        public int TotalOutgoingDocuments { get; set; }
        public int TotalInternalDocuments { get; set; }
        public int TotalInstitutions { get; set; }
        public int CurrentMonthDocuments { get; set; }
        public int CurrentWeekDocuments { get; set; }
        public int TodayDocuments { get; set; }
        public List<MonthlyDocumentCount> MonthlyData { get; set; } = new();
        public int MaxMonthlyCount { get; set; } = 1;
        public int SelectedYear { get; set; } = DateTime.Now.Year;
        public List<TopInstitution> TopInstitutions { get; set; } = new();
        public List<TopUser> TopUsers { get; set; } = new();
    }
}