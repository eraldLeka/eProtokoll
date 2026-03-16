using eProtokoll.Models;

namespace eProtokoll.Repositories.Dashboard
{
    public class DashboardStats
    {
        public int TotalIncoming { get; set; }
        public int TotalOutgoing { get; set; }
        public int TotalInternal { get; set; }
        public int TotalDocuments => TotalIncoming + TotalOutgoing + TotalInternal;
    }

    public class AdminStats
    {
        public int TotalUsers { get; set; }
        public int TotalDocuments { get; set; }
        public int DocumentsToday { get; set; }
        public int TotalInstitutions { get; set; }
        public int UsersThisMonth { get; set; }
        public int InstitutionsThisMonth { get; set; }
        public int Incoming { get; set; }
        public int Outgoing { get; set; }
        public int Internal { get; set; }
        public int Total => Incoming + Outgoing + Internal;
        public int IncomingPct => Total > 0 ? Incoming * 100 / Total : 0;
        public int OutgoingPct => Total > 0 ? Outgoing * 100 / Total : 0;
        public int InternalPct => Total > 0 ? Internal * 100 / Total : 0;
    }

    public class DailyCount
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyCount
    {
        public string MonthName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RecentActivity
    {
        public string Time { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ProtocolNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DocumentType DocumentType { get; set; }
    }

    public interface IDashboardRepository
    {
        Task<DashboardStats> GetDocumentStatsAsync(int? userId = null);
        Task<List<eProtokoll.Models.Document>> GetRecentDocumentsAsync(int? userId = null);
        Task<List<DailyCount>> GetDailyStatsAsync(int days = 7, int? userId = null);
        Task<AdminStats> GetAdminStatsAsync();
        Task<List<RecentActivity>> GetRecentActivityAsync();
        Task<List<MonthlyCount>> GetMonthlyDataAsync();
    }
}