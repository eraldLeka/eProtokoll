using eProtokoll.Models;

namespace eProtokoll.Repositories
{
    public interface IReportRepository
    {
        // ==================== DOKUMENTET ====================
        Task<int> GetTotalDocumentsAsync();
        Task<int> GetTotalByTypeAsync(DocumentType type);

        // ==================== INSTITUCIONET ====================
        Task<int> GetTotalInstitutionsAsync();

        // ==================== KOHORE ====================
        Task<int> GetCurrentMonthDocumentsAsync();
        Task<int> GetCurrentWeekDocumentsAsync();
        Task<int> GetTodayDocumentsAsync();

        // ==================== PRIORITETI ====================
        Task<int> GetByPriorityAsync(Priority priority);

        // ==================== GRAFIKU & LISTAT ====================
        Task<List<MonthlyDocumentCount>> GetMonthlyDocumentCountsAsync(int year);
        Task<List<TopInstitution>> GetTopInstitutionsAsync(int topCount = 5);
        Task<List<TopUser>> GetTopUsersAsync(int topCount = 5);
    }

    public class MonthlyDocumentCount
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopInstitution
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
        public int Incoming { get; set; }
        public int Outgoing { get; set; }
    }

    public class TopUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
    }
}