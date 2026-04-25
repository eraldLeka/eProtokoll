using eProtokoll.Models;
using eProtokoll.Repositories;

namespace eProtokoll.Services.Report
{
    public class ReportService
    {
        private readonly IReportRepository _repo;

        public ReportService(IReportRepository repo)
        {
            _repo = repo;
        }

        // ================= TOTALS =================

        public Task<int> GetTotalDocumentsAsync(string role)
            => _repo.GetTotalDocumentsAsync();

        public Task<int> GetTotalByTypeAsync(DocumentType type, string role)
            => _repo.GetTotalByTypeAsync(type);

        public Task<int> GetTotalByPriorityAsync(Priority priority, string role)
            => _repo.GetTotalByPriorityAsync(priority);

        public Task<int> GetUsersAsync()
            => _repo.GetTotalUsersAsync();

        public Task<int> GetInstitutionsAsync()
            => _repo.GetTotalInstitutionsAsync();

        // ================= TIME =================

        public Task<int> GetTodayAsync(string role)
            => _repo.GetTodayDocumentsAsync();

        public Task<int> GetWeekAsync(string role)
            => _repo.GetCurrentWeekDocumentsAsync();

        public Task<int> GetMonthAsync(string role)
            => _repo.GetCurrentMonthDocumentsAsync();

        // ================= TOP =================

        public Task<List<TopUser>> GetTopUsersAsync(string role)
            => _repo.GetTopUsersAsync(5);

        public Task<List<TopInstitution>> GetTopInstitutionsAsync(string role)
            => _repo.GetTopInstitutionsAsync(5);
    }
}