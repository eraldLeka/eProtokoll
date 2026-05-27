using eProtokoll.Models;

namespace eProtokoll.Repositories
{
    public interface ITrackingRepository
    {
        // ===== INDEX =====
        Task<(List<DocumentTracking> Trackings, int TotalCount)> GetAllAsync(
            int page, int pageSize);

        Task<(List<DocumentTracking> Trackings, int TotalCount)> GetByUserAsync(
            int page, int pageSize, int userId);

        // ===== DETAILS =====
        Task<DocumentTracking?> GetByIdAsync(int trackingId);

        // ===== ASSIGN =====
        Task InsertAsync(DocumentTracking model, int assignedByUserId);

        // ===== COMPLETE =====
        Task CompleteAsync(int trackingId);

        // ===== NOTIFICATIONS (EMPLOYEE) =====
        Task<int> GetOverdueCountAsync(int userId);
        Task<List<DocumentTracking>> GetNotificationItemsAsync(int userId, int take = 10);

        // ===== DROPDOWNS =====
        Task<List<Document>> GetDocumentsForDropdownAsync();
        Task<List<Users>> GetEmployeesAsync();
    }
}