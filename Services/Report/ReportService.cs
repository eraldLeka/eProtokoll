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

        private static bool IsPrivilegedRole(string role)
            => role == "Administrator" || role == "Manager";

        public Task<int> GetTotalDocumentsAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTotalDocumentsAsync()
                : _repo.GetVisibleTotalDocumentsAsync(userId);

        public Task<int> GetTotalByTypeAsync(DocumentType type, string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTotalByTypeAsync(type)
                : _repo.GetVisibleTotalByTypeAsync(type, userId);

        public Task<int> GetTotalByPriorityAsync(Priority priority, string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTotalByPriorityAsync(priority)
                : _repo.GetVisibleTotalByPriorityAsync(priority, userId);

        public Task<int> GetUsersAsync()
            => _repo.GetTotalUsersAsync();

        public Task<int> GetInstitutionsAsync()
            => _repo.GetTotalInstitutionsAsync();

        // ================= TIME =================

        public Task<int> GetTodayAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTodayDocumentsAsync()
                : _repo.GetVisibleTodayDocumentsAsync(userId);

        public Task<int> GetWeekAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetCurrentWeekDocumentsAsync()
                : _repo.GetVisibleCurrentWeekDocumentsAsync(userId);

        public Task<int> GetMonthAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetCurrentMonthDocumentsAsync()
                : _repo.GetVisibleCurrentMonthDocumentsAsync(userId);

        // ================= TRACKING =================

        public Task<int> GetTrackingActiveAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTrackingActiveCountAsync()
                : _repo.GetVisibleTrackingActiveCountAsync(userId);

        public Task<int> GetTrackingOverdueAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTrackingOverdueCountAsync()
                : _repo.GetVisibleTrackingOverdueCountAsync(userId);

        public Task<int> GetTrackingCompletedAsync(string role, int userId)
            => IsPrivilegedRole(role)
                ? _repo.GetTrackingCompletedCountAsync()
                : _repo.GetVisibleTrackingCompletedCountAsync(userId);

        // ================= TOP =================

        public Task<List<TopUser>> GetTopUsersAsync(string role)
            => _repo.GetTopUsersAsync(5);

        public Task<List<TopInstitution>> GetTopInstitutionsAsync(string role)
            => _repo.GetTopInstitutionsAsync(5);
    }
}