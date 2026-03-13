using eProtokoll.Models;

namespace eProtokoll.Repositories
{
    public interface IReportRepository
    {
        // ===== STATISTIKA DOKUMENTESH =====
        Task<int> GetTotalDocumentsAsync();
        Task<int> GetTotalByDiscriminatorAsync(string discriminator);

        // ===== STATISTIKA INSTITUCIONESH =====
        Task<int> GetTotalInstitutionsAsync();
        Task<int> GetActiveInstitutionsAsync();

        // ===== STATISTIKA PRIORITETIT =====
        Task<int> GetTotalByPriorityAsync(Priority priority);

        // ===== STATISTIKA KOHORE =====
        Task<int> GetCurrentMonthDocumentsAsync();
        Task<int> GetCurrentWeekDocumentsAsync();
        Task<int> GetTodayDocumentsAsync();

        // ===== STATISTIKA TRACKING =====
        Task<int> GetActiveTrackingsAsync();
        Task<int> GetCompletedTrackingsAsync();

        // ===== AUDIT LOG =====
        Task<List<DocumentTracking>> GetAuditLogAsync();
    }
}