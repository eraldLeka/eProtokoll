using eProtokoll.Models;

namespace eProtokoll.Repositories
{
    public interface IReportRepository
    {
        // ================= TOTALS =================
        Task<int> GetTotalDocumentsAsync();
        Task<int> GetTotalByTypeAsync(DocumentType type);
        Task<int> GetTotalByPriorityAsync(Priority priority);
        Task<int> GetVisibleTotalDocumentsAsync(int userId);
        Task<int> GetVisibleTotalByTypeAsync(DocumentType type, int userId);
        Task<int> GetVisibleTotalByPriorityAsync(Priority priority, int userId);
        Task<int> GetTotalUsersAsync();
        Task<int> GetTotalInstitutionsAsync();

        // ================= TIME =================
        Task<int> GetTodayDocumentsAsync();
        Task<int> GetCurrentWeekDocumentsAsync();
        Task<int> GetCurrentMonthDocumentsAsync();
        Task<int> GetVisibleTodayDocumentsAsync(int userId);
        Task<int> GetVisibleCurrentWeekDocumentsAsync(int userId);
        Task<int> GetVisibleCurrentMonthDocumentsAsync(int userId);

        // ================= TRACKING =================
        Task<int> GetTrackingActiveCountAsync();
        Task<int> GetTrackingOverdueCountAsync();
        Task<int> GetTrackingCompletedCountAsync();
        Task<int> GetVisibleTrackingActiveCountAsync(int userId);
        Task<int> GetVisibleTrackingOverdueCountAsync(int userId);
        Task<int> GetVisibleTrackingCompletedCountAsync(int userId);

        // ================= TOP =================
        Task<List<TopUser>> GetTopUsersAsync(int topCount);
        Task<List<TopInstitution>> GetTopInstitutionsAsync(int topCount);
    }

    public class TopInstitution
    {
        public int InstitutionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
        public int Incoming { get; set; }
        public int Outgoing { get; set; }
    }

    public class TopUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public Users.UserRole Role { get; set; }
        public int TotalDocuments { get; set; }
    }
}