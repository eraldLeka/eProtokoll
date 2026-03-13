using eProtokoll.Models;
namespace eProtokoll.Repositories.Document
{
    public interface IDocumentRepository
    {
        // Incoming documents
        Task<(List<IncomingDocument> Documents, int TotalCount)> GetIncomingAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null);
        Task<IncomingDocument?> GetIncomingByIdAsync(int id);
        Task<int> InsertIncomingAsync(IncomingDocument model);
        // Outgoing documents
        Task<(List<OutgoingDocument> Documents, int TotalCount)> GetOutgoingAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null);
        Task<OutgoingDocument?> GetOutgoingByIdAsync(int id);
        Task<int> InsertOutgoingAsync(OutgoingDocument model);
        // Internal documents
        Task<(List<InternalDocument> Documents, int TotalCount)> GetInternalAsync(
            int page, int pageSize, int? createdBy = null, int? accessUserId = null);
        Task<InternalDocument?> GetInternalByIdAsync(int id);
        Task<int> InsertInternalAsync(InternalDocument model);
        // Common
        Task<int> GetCountAsync(DocumentType type);
        Task<int> GetTodayCountAsync(DocumentType type);
        // Attachments
        Task InsertAttachmentAsync(DocumentAttachment attachment);
        Task<List<DocumentAttachment>> GetAttachmentsByDocumentIdAsync(int documentId);
        // Dropdown
        Task<List<Institution>> GetInstitutionsAsync();
        // Users
        Task<List<Users>> GetActiveUsersAsync();
        // Permissions
        Task InsertDocumentPermissionsAsync(int documentId, List<int> userIds);
    }
}