using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace eProtokoll.Repositories.Documents
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly string _connectionString;
        public DocumentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ================= GENERIC QUERY ENGINE =================

        public async Task<(List<T> Documents, int TotalCount)> GetDocumentsAsync<T>(
            string baseQuery,
            string countQuery,
            SqlParameter[] parameters,
            Func<SqlDataReader, T> mapper)
        {
            var list = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int totalCount;

            using (var countCmd = new SqlCommand(countQuery, connection))
            {
                countCmd.Parameters.AddRange(CloneParameters(parameters));
                totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            using (var cmd = new SqlCommand(baseQuery, connection))
            {
                cmd.Parameters.AddRange(CloneParameters(parameters));

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(mapper(reader));
                }
            }

            return (list, totalCount);
        }

        private static SqlParameter[] CloneParameters(SqlParameter[] parameters)
            => parameters
                .Select(p => (SqlParameter)((ICloneable)p).Clone())
                .ToArray();

        private static bool IsPrivilegedRole(string role)
            => role == "Admin" || role == "Manager" || role == "Administrator";

        // ================= INSERT =================

        public async Task<int> InsertAsync(
            string query,
            SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                cmd.Parameters.AddRange(parameters);

                var result = await cmd.ExecuteScalarAsync();

                transaction.Commit();
                return Convert.ToInt32(result);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ================= SINGLE OBJECT =================

        public async Task<T?> GetSingleAsync<T>(
            string query,
            SqlParameter[] parameters,
            Func<SqlDataReader, T> mapper)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddRange(parameters);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return mapper(reader);

            return default;
        }

        // ================= DETAILS BY ID =================

        public Task<(List<IncomingDocument> Documents, int TotalCount)> GetIncomingAsync(int page, int pageSize, int userId, string role)
            => GetDocumentsAsync(
                @"SELECT d.*,
                         u.FirstName AS CreatorFirstName,
                         u.LastName AS CreatorLastName,
                         u.UserName AS CreatorUserName,
                         i.Name AS InstitutionName,
                         i.Adress AS InstitutionAdress,
                         la.AttachmentId AS LatestAttachmentId,
                         la.OriginalFileName AS LatestAttachmentName,
                         la.FilePath AS LatestAttachmentPath,
                         la.FileExtension AS LatestAttachmentExtension,
                         la.FileSize AS LatestAttachmentSize,
                         la.UploadedDate AS LatestAttachmentUploadedDate,
                         la.UploadedBy AS LatestAttachmentUploadedBy,
                         la.Category AS LatestAttachmentCategory,
                         CASE WHEN da.DocumentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAttachments
                  FROM Documents d
                  LEFT JOIN Users u ON u.Id = d.CreatedBy
                  LEFT JOIN Institutions i ON i.InstitutionId = d.InstitutionId
                  OUTER APPLY (
                      SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                   a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                      FROM DocumentAttachments a
                      WHERE a.DocumentId = d.DocumentId
                      ORDER BY a.UploadedDate DESC
                  ) la
                  LEFT JOIN (SELECT DISTINCT DocumentId FROM DocumentAttachments) da ON da.DocumentId = d.DocumentId
                  WHERE d.DocumentType = @DocumentType
                    AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR d.CreatedBy = @UserId
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                    )
                  ORDER BY d.Year DESC, d.DocumentNumber DESC
                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                @"SELECT COUNT(*)
                  FROM Documents d
                  WHERE d.DocumentType = @DocumentType
                    AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR d.CreatedBy = @UserId
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                    )",
                new[]
                {
                    new SqlParameter("@DocumentType", (int)DocumentType.Incoming),
                    new SqlParameter("@IsPrivileged", IsPrivilegedRole(role) ? 1 : 0),
                    new SqlParameter("@PublicClassification", (int)Classification.Public),
                    new SqlParameter("@ConfidentialClassification", (int)Classification.Confidential),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Offset", (page - 1) * pageSize),
                    new SqlParameter("@PageSize", pageSize)
                },
                DocumentMapper.MapToIncomingDocument);

        public Task<(List<OutgoingDocument> Documents, int TotalCount)> GetOutgoingAsync(int page, int pageSize, int userId, string role)
            => GetDocumentsAsync(
                @"SELECT d.*,
                         u.FirstName AS CreatorFirstName,
                         u.LastName AS CreatorLastName,
                         u.UserName AS CreatorUserName,
                         i.Name AS InstitutionName,
                         i.Adress AS InstitutionAdress,
                         la.AttachmentId AS LatestAttachmentId,
                         la.OriginalFileName AS LatestAttachmentName,
                         la.FilePath AS LatestAttachmentPath,
                         la.FileExtension AS LatestAttachmentExtension,
                         la.FileSize AS LatestAttachmentSize,
                         la.UploadedDate AS LatestAttachmentUploadedDate,
                         la.UploadedBy AS LatestAttachmentUploadedBy,
                         la.Category AS LatestAttachmentCategory,
                         CASE WHEN da.DocumentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAttachments
                  FROM Documents d
                  LEFT JOIN Users u ON u.Id = d.CreatedBy
                  LEFT JOIN Institutions i ON i.InstitutionId = d.InstitutionId
                  OUTER APPLY (
                      SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                   a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                      FROM DocumentAttachments a
                      WHERE a.DocumentId = d.DocumentId
                      ORDER BY a.UploadedDate DESC
                  ) la
                  LEFT JOIN (SELECT DISTINCT DocumentId FROM DocumentAttachments) da ON da.DocumentId = d.DocumentId
                  WHERE d.DocumentType = @DocumentType
                    AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR d.CreatedBy = @UserId
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                    )
                  ORDER BY d.Year DESC, d.DocumentNumber DESC
                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                @"SELECT COUNT(*)
                  FROM Documents d
                  WHERE d.DocumentType = @DocumentType
                    AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR d.CreatedBy = @UserId
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                    )",
                new[]
                {
                    new SqlParameter("@DocumentType", (int)DocumentType.Outgoing),
                    new SqlParameter("@IsPrivileged", IsPrivilegedRole(role) ? 1 : 0),
                    new SqlParameter("@PublicClassification", (int)Classification.Public),
                    new SqlParameter("@ConfidentialClassification", (int)Classification.Confidential),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Offset", (page - 1) * pageSize),
                    new SqlParameter("@PageSize", pageSize)
                },
                DocumentMapper.MapToOutgoingDocument);

        public Task<(List<InternalDocument> Documents, int TotalCount)> GetInternalAsync(int page, int pageSize, int userId, string role)
            => GetDocumentsAsync(
                @"SELECT d.*,
                         u.FirstName AS CreatorFirstName,
                         u.LastName AS CreatorLastName,
                         u.UserName AS CreatorUserName,
                         la.AttachmentId AS LatestAttachmentId,
                         la.OriginalFileName AS LatestAttachmentName,
                         la.FilePath AS LatestAttachmentPath,
                         la.FileExtension AS LatestAttachmentExtension,
                         la.FileSize AS LatestAttachmentSize,
                         la.UploadedDate AS LatestAttachmentUploadedDate,
                         la.UploadedBy AS LatestAttachmentUploadedBy,
                         la.Category AS LatestAttachmentCategory,
                         CASE WHEN da.DocumentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAttachments
                  FROM Documents d
                  LEFT JOIN Users u ON u.Id = d.CreatedBy
                  OUTER APPLY (
                      SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                   a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                      FROM DocumentAttachments a
                      WHERE a.DocumentId = d.DocumentId
                      ORDER BY a.UploadedDate DESC
                  ) la
                  LEFT JOIN (SELECT DISTINCT DocumentId FROM DocumentAttachments) da ON da.DocumentId = d.DocumentId
                  WHERE d.DocumentType = @DocumentType
                    AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR d.CreatedBy = @UserId
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                    )
                  ORDER BY d.Year DESC, d.DocumentNumber DESC
                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                @"SELECT COUNT(*)
                  FROM Documents d
                  WHERE d.DocumentType = @DocumentType
                    AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR d.CreatedBy = @UserId
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                    )",
                new[]
                {
                    new SqlParameter("@DocumentType", (int)DocumentType.Internal),
                    new SqlParameter("@IsPrivileged", IsPrivilegedRole(role) ? 1 : 0),
                    new SqlParameter("@PublicClassification", (int)Classification.Public),
                    new SqlParameter("@ConfidentialClassification", (int)Classification.Confidential),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Offset", (page - 1) * pageSize),
                    new SqlParameter("@PageSize", pageSize)
                },
                DocumentMapper.MapToInternalDocument);

        public async Task<IncomingDocument?> GetIncomingByIdAsync(int id)
        {
            var document = await GetSingleAsync(
                @"SELECT d.*,
                         u.FirstName AS CreatorFirstName,
                         u.LastName AS CreatorLastName,
                         u.UserName AS CreatorUserName,
                         i.Name AS InstitutionName,
                         i.Adress AS InstitutionAdress,
                         CASE WHEN da.DocumentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAttachments
                  FROM Documents d
                  LEFT JOIN Users u ON u.Id = d.CreatedBy
                  LEFT JOIN Institutions i ON i.InstitutionId = d.InstitutionId
                  LEFT JOIN (SELECT DISTINCT DocumentId FROM DocumentAttachments) da ON da.DocumentId = d.DocumentId
                  WHERE d.DocumentId = @Id
                    AND d.DocumentType = @DocumentType",
                new[]
                {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@DocumentType", (int)DocumentType.Incoming)
                },
                DocumentMapper.MapToIncomingDocument);

            if (document == null) return null;

            document.Attachments = await GetDocumentAttachmentsAsync(id);
            document.HasAttachments = document.Attachments.Any();

            return document;
        }

        public async Task<OutgoingDocument?> GetOutgoingByIdAsync(int id)
        {
            var document = await GetSingleAsync(
                @"SELECT d.*,
                         u.FirstName AS CreatorFirstName,
                         u.LastName AS CreatorLastName,
                         u.UserName AS CreatorUserName,
                         i.Name AS InstitutionName,
                         i.Adress AS InstitutionAdress,
                         CASE WHEN da.DocumentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAttachments
                  FROM Documents d
                  LEFT JOIN Users u ON u.Id = d.CreatedBy
                  LEFT JOIN Institutions i ON i.InstitutionId = d.InstitutionId
                  LEFT JOIN (SELECT DISTINCT DocumentId FROM DocumentAttachments) da ON da.DocumentId = d.DocumentId
                  WHERE d.DocumentId = @Id
                    AND d.DocumentType = @DocumentType",
                new[]
                {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@DocumentType", (int)DocumentType.Outgoing)
                },
                DocumentMapper.MapToOutgoingDocument);

            if (document == null) return null;

            document.Attachments = await GetDocumentAttachmentsAsync(id);
            document.HasAttachments = document.Attachments.Any();

            return document;
        }

        public async Task<InternalDocument?> GetInternalByIdAsync(int id)
        {
            var document = await GetSingleAsync(
                @"SELECT d.*,
                         u.FirstName AS CreatorFirstName,
                         u.LastName AS CreatorLastName,
                         u.UserName AS CreatorUserName,
                         CASE WHEN da.DocumentId IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAttachments
                  FROM Documents d
                  LEFT JOIN Users u ON u.Id = d.CreatedBy
                  LEFT JOIN (SELECT DISTINCT DocumentId FROM DocumentAttachments) da ON da.DocumentId = d.DocumentId
                  WHERE d.DocumentId = @Id
                    AND d.DocumentType = @DocumentType",
                new[]
                {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@DocumentType", (int)DocumentType.Internal)
                },
                DocumentMapper.MapToInternalDocument);

            if (document == null) return null;

            document.Attachments = await GetDocumentAttachmentsAsync(id);
            document.HasAttachments = document.Attachments.Any();

            return document;
        }

        private Task<List<DocumentAttachment>> GetDocumentAttachmentsAsync(int documentId)
            => GetAttachmentsAsync(
                @"SELECT AttachmentId, DocumentId, OriginalFileName, FilePath, FileSize,
                         FileExtension, UploadedDate, UploadedBy, Category, Description
                  FROM DocumentAttachments
                  WHERE DocumentId = @DocumentId
                  ORDER BY UploadedDate DESC",
                new[] { new SqlParameter("@DocumentId", documentId) });

        // ================= ATTACHMENTS =================

        public async Task InsertAttachmentAsync(
            string query,
            SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmd = new SqlCommand(query, connection, transaction);
                cmd.Parameters.AddRange(parameters);

                await cmd.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<DocumentAttachment>> GetAttachmentsAsync(
            string query,
            SqlParameter[] parameters)
        {
            var list = new List<DocumentAttachment>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddRange(parameters);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(AttachmentMapper.Map(reader));
            }

            return list;
        }

        public async Task<DocumentAttachment?> GetAttachmentByIdAsync(int attachmentId)
        {
            var attachments = await GetAttachmentsAsync(
                @"SELECT TOP 1 AttachmentId, DocumentId, OriginalFileName, FilePath, FileSize,
                         FileExtension, UploadedDate, UploadedBy, Category, Description
                  FROM DocumentAttachments
                  WHERE AttachmentId = @AttachmentId",
                new[] { new SqlParameter("@AttachmentId", attachmentId) });

            return attachments.FirstOrDefault();
        }

        // ================= PERMISSIONS =================

        public async Task InsertDocumentPermissionsAsync(int documentId, List<int> userIds)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var userId in userIds)
                {
                    using var cmd = new SqlCommand(
                        @"INSERT INTO DocumentPermissions (DocumentId, UserId)
                          VALUES (@DocumentId, @UserId)", connection, transaction);

                    cmd.Parameters.AddWithValue("@DocumentId", documentId);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ================= NOTIFICATIONS =================

        public async Task<int> GetNewVisibleDocumentsCountAsync(int userId, string role)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Documents d
                WHERE d.CreatedBy <> @UserId
                  AND CAST(d.CreatedDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                  )", connection);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@IsPrivileged", IsPrivilegedRole(role) ? 1 : 0);
            cmd.Parameters.AddWithValue("@PublicClassification", (int)Classification.Public);
            cmd.Parameters.AddWithValue("@ConfidentialClassification", (int)Classification.Confidential);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<Document>> GetNewVisibleDocumentsAsync(int userId, string role, int take = 10)
        {
            var documents = new List<Document>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT TOP (@Take)
                    d.DocumentId, d.DocumentNumber, d.Year,
                    d.DocumentType, d.Subject, d.CreatedDate
                FROM Documents d
                WHERE d.CreatedBy <> @UserId
                  AND CAST(d.CreatedDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND (
                        @IsPrivileged = 1
                        OR d.Classification = @PublicClassification
                        OR (
                            d.Classification = @ConfidentialClassification
                            AND EXISTS (
                                SELECT 1
                                FROM DocumentPermissions dp
                                WHERE dp.DocumentId = d.DocumentId
                                  AND dp.UserId = @UserId
                            )
                        )
                  )
                ORDER BY d.CreatedDate DESC", connection);

            cmd.Parameters.AddWithValue("@Take", take);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@IsPrivileged", IsPrivilegedRole(role) ? 1 : 0);
            cmd.Parameters.AddWithValue("@PublicClassification", (int)Classification.Public);
            cmd.Parameters.AddWithValue("@ConfidentialClassification", (int)Classification.Confidential);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                documents.Add(new Document
                {
                    DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                    DocumentNumber = reader.GetInt32(reader.GetOrdinal("DocumentNumber")),
                    Year = reader.GetInt32(reader.GetOrdinal("Year")),
                    DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                    Subject = reader.IsDBNull(reader.GetOrdinal("Subject"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("Subject")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
                });
            }

            return documents;
        }

        // ================= DROPDOWNS =================

        public async Task<List<Institution>> GetInstitutionsAsync()
        {
            var list = new List<Institution>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                @"SELECT InstitutionId, Name, ShortName 
                  FROM Institutions 
                  WHERE IsActive = 1 
                  ORDER BY Name",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Institution
                {
                    InstitutionId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    ShortName = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }

            return list;
        }

        public async Task<List<Users>> GetActiveUsersAsync()
        {
            var list = new List<Users>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                @"SELECT Id, FirstName, LastName, UserName
                  FROM Users
                  WHERE IsActive = 1 AND Role = 3
                  ORDER BY FirstName, LastName",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Users
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    UserName = reader.GetString(3)
                });
            }

            return list;
        }
    }
}