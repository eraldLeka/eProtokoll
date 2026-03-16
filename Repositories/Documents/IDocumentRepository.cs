using eProtokoll.Models;

namespace eProtokoll.Repositories.Documents
{
    public interface IDocumentRepository
    {
        // ================= INCOMING =================

        Task<(List<IncomingDocument> Documents, int TotalCount)> GetIncomingAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null);

        Task<IncomingDocument?> GetIncomingByIdAsync(int id);

        Task<int> InsertIncomingAsync(IncomingDocument model);

        // ================= OUTGOING =================

        Task<(List<OutgoingDocument> Documents, int TotalCount)> GetOutgoingAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null);

        Task<OutgoingDocument?> GetOutgoingByIdAsync(int id);

        Task<int> InsertOutgoingAsync(OutgoingDocument model);

        // ================= INTERNAL =================

        Task<(List<InternalDocument> Documents, int TotalCount)> GetInternalAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null);

        Task<InternalDocument?> GetInternalByIdAsync(int id);

        Task<int> InsertInternalAsync(InternalDocument model);

        // ================= STATISTICS =================

        Task<int> GetCountAsync(DocumentType type);

        Task<int> GetTodayCountAsync(DocumentType type);

        // ================= ATTACHMENTS =================

        Task InsertAttachmentAsync(DocumentAttachment attachment);

        Task<List<DocumentAttachment>> GetAttachmentsByDocumentIdAsync(int documentId);

        // ================= DROPDOWNS =================

        Task<List<Institution>> GetInstitutionsAsync();

        Task<List<Users>> GetActiveUsersAsync();

        // ================= PERMISSIONS =================

        Task InsertDocumentPermissionsAsync(int documentId, List<int> userIds);
    }
}