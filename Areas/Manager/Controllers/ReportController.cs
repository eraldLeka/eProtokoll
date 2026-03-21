using eProtokoll.Models;
using eProtokoll.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class ReportController : Controller
    {
        private readonly IReportRepository _reportRepository;

        public ReportController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<IActionResult> Index()
        {
            var year = DateTime.Now.Year;

            // === TOTALET ===
            ViewBag.TotalDocuments = await _reportRepository.GetTotalDocumentsAsync();
            ViewBag.TotalIncoming = await _reportRepository.GetTotalByTypeAsync(DocumentType.Incoming);
            ViewBag.TotalOutgoing = await _reportRepository.GetTotalByTypeAsync(DocumentType.Outgoing);
            ViewBag.TotalInternal = await _reportRepository.GetTotalByTypeAsync(DocumentType.Internal);

            // === SIPAS PERIUDHËS ===
            ViewBag.TodayCount = await _reportRepository.GetTodayDocumentsAsync();
            ViewBag.MonthCount = await _reportRepository.GetCurrentMonthDocumentsAsync();
            ViewBag.WeekCount = await _reportRepository.GetCurrentWeekDocumentsAsync();

            // === PRIORITETI ===
            ViewBag.High = await _reportRepository.GetByPriorityAsync(Priority.High);
            ViewBag.Normal = await _reportRepository.GetByPriorityAsync(Priority.Normal);
            ViewBag.Low = await _reportRepository.GetByPriorityAsync(Priority.Low);

            // === GRAFIKU MUJOR ===
            ViewBag.MonthlyData = await _reportRepository.GetMonthlyDocumentCountsAsync(year);

            // === TOP 5 INSTITUCIONET ===
            ViewBag.TopInstitutions = await _reportRepository.GetTopInstitutionsAsync(5);

            // === TOP 5 PËRDORUESIT ===
            ViewBag.TopUsers = await _reportRepository.GetTopUsersAsync(5);

            return View();
        }
    }
}