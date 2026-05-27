using eProtokoll.Models;

namespace eProtokoll.Repositories.Dashboard
{
    public class DailyCount
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public interface IDashboardRepository
    {
        Task<List<Document>> GetRecentDocumentsAsync(int? userId = null);
        Task<List<DailyCount>> GetDailyStatsAsync(int days = 7, int? userId = null);
    }
}