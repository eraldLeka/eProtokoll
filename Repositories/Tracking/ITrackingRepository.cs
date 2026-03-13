using eProtokoll.Models;
using DocModel = eProtokoll.Models.Document;

namespace eProtokoll.Repositories
{
    public interface ITrackingRepository
    {
        // ===== INDEX =====
        Task<(List<DocumentTracking> Trackings, int TotalCount)> GetAllAsync(
            int page, int pageSize, string searchTerm = "");

        Task<(List<DocumentTracking> Trackings, int TotalCount)> GetByUserAsync(
            int page, int pageSize, int userId);

        // ===== DETAILS =====
        Task<DocumentTracking?> GetByIdAsync(int trackingId);

        // ===== ASSIGN =====
        Task InsertAsync(DocumentTracking model, int assignedByUserId);

        // ===== COMPLETE / CANCEL =====
        Task CompleteAsync(int trackingId);
        Task CancelAsync(int trackingId, string reason);

        // ===== DROPDOWNS =====
        Task<List<DocModel>> GetDocumentsForDropdownAsync();
        Task<List<Users>> GetEmployeesAsync();
    }
}