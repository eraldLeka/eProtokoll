using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eProtokoll.Repositories.Documents
{
    public interface IDocumentRepository
    {
        // ================= GENERIC ENGINE =================

        Task<(List<T> Documents, int TotalCount)> GetDocumentsAsync<T>(
            string baseQuery,
            string countQuery,
            SqlParameter[] parameters,
            Func<SqlDataReader, T> mapper);

        Task<int> InsertAsync(
            string query,
            SqlParameter[] parameters);

        Task<T?> GetSingleAsync<T>(
            string query,
            SqlParameter[] parameters,
            Func<SqlDataReader, T> mapper);

        // ================= DETAILS BY ID =================

        Task<(List<IncomingDocument> Documents, int TotalCount)> GetIncomingAsync(int page, int pageSize, int userId, string role);
        Task<(List<OutgoingDocument> Documents, int TotalCount)> GetOutgoingAsync(int page, int pageSize, int userId, string role);
        Task<(List<InternalDocument> Documents, int TotalCount)> GetInternalAsync(int page, int pageSize, int userId, string role);

        Task<IncomingDocument?> GetIncomingByIdAsync(int id);
        Task<OutgoingDocument?> GetOutgoingByIdAsync(int id);
        Task<InternalDocument?> GetInternalByIdAsync(int id);

        // ================= ATTACHMENTS =================

        Task InsertAttachmentAsync(
            string query,
            SqlParameter[] parameters);

        Task<List<DocumentAttachment>> GetAttachmentsAsync(
            string query,
            SqlParameter[] parameters);

        Task<DocumentAttachment?> GetAttachmentByIdAsync(int attachmentId);

        // ================= PERMISSIONS =================

        Task InsertDocumentPermissionsAsync(int documentId, List<int> userIds);

        // ================= DROPDOWNS =================

        Task<int> GetNewVisibleDocumentsCountAsync(int userId, string role);
        Task<List<Document>> GetNewVisibleDocumentsAsync(int userId, string role, int take = 10);

        Task<List<Institution>> GetInstitutionsAsync();

        Task<List<Users>> GetActiveUsersAsync();
    }
}