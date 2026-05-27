using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;
using System.Text;

namespace eProtokoll.Repositories.ProtocolBook
{
    public class ProtocolBookRepository : IProtocolBookRepository
    {
        private readonly string _connectionString;

        public ProtocolBookRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<(List<Document> Documents, int TotalItems)> GetPagedAsync(
            int page,
            int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var totalItems = await GetCount(connection,
                "SELECT COUNT(*) FROM Documents d");

            var sql = @"
                SELECT d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName,
                       la.AttachmentId AS LatestAttachmentId,
                       la.OriginalFileName AS LatestAttachmentName,
                       la.FilePath AS LatestAttachmentPath,
                       la.FileExtension AS LatestAttachmentExtension,
                       la.FileSize AS LatestAttachmentSize,
                       la.UploadedDate AS LatestAttachmentUploadedDate,
                       la.UploadedBy AS LatestAttachmentUploadedBy,
                       la.Category AS LatestAttachmentCategory
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                OUTER APPLY (
                    SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                 a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                    FROM DocumentAttachments a
                    WHERE a.DocumentId = d.DocumentId
                    ORDER BY a.UploadedDate DESC
                ) la
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var documents = await ReadDocuments(connection, sql, page, pageSize);

            return (documents, totalItems);
        }

        public async Task<(List<Document> Documents, int TotalItems)> GetPagedForEmployeeAsync(
            int page,
            int pageSize,
            int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder(@"
                WHERE (
                    d.Classification = 1
                    OR d.CreatedBy = @UserId
                    OR (d.Classification = 2 AND EXISTS (
                        SELECT 1 FROM DocumentPermissions dp
                        WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                    ))
                )");

            var countSql = $"SELECT COUNT(*) FROM Documents d {where}";
            int totalItems;

            using (var cmd = new SqlCommand(countSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                totalItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var sql = $@"
                SELECT d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName,
                       la.AttachmentId AS LatestAttachmentId,
                       la.OriginalFileName AS LatestAttachmentName,
                       la.FilePath AS LatestAttachmentPath,
                       la.FileExtension AS LatestAttachmentExtension,
                       la.FileSize AS LatestAttachmentSize,
                       la.UploadedDate AS LatestAttachmentUploadedDate,
                       la.UploadedBy AS LatestAttachmentUploadedBy,
                       la.Category AS LatestAttachmentCategory
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                OUTER APPLY (
                    SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                 a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                    FROM DocumentAttachments a
                    WHERE a.DocumentId = d.DocumentId
                    ORDER BY a.UploadedDate DESC
                ) la
                {where}
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var documents = new List<Document>();

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    documents.Add(MapDocument(reader));
            }

            return (documents, totalItems);
        }

        public async Task<List<Document>> GetForPrintAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName,
                       la.AttachmentId AS LatestAttachmentId,
                       la.OriginalFileName AS LatestAttachmentName,
                       la.FilePath AS LatestAttachmentPath,
                       la.FileExtension AS LatestAttachmentExtension,
                       la.FileSize AS LatestAttachmentSize,
                       la.UploadedDate AS LatestAttachmentUploadedDate,
                       la.UploadedBy AS LatestAttachmentUploadedBy,
                       la.Category AS LatestAttachmentCategory
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                OUTER APPLY (
                    SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                 a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                    FROM DocumentAttachments a
                    WHERE a.DocumentId = d.DocumentId
                    ORDER BY a.UploadedDate DESC
                ) la
                ORDER BY d.Year ASC, d.DocumentNumber ASC";

            var documents = new List<Document>();

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                documents.Add(MapDocument(reader));

            return documents;
        }

        public async Task<List<Document>> GetForPrintForEmployeeAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName,
                       la.AttachmentId AS LatestAttachmentId,
                       la.OriginalFileName AS LatestAttachmentName,
                       la.FilePath AS LatestAttachmentPath,
                       la.FileExtension AS LatestAttachmentExtension,
                       la.FileSize AS LatestAttachmentSize,
                       la.UploadedDate AS LatestAttachmentUploadedDate,
                       la.UploadedBy AS LatestAttachmentUploadedBy,
                       la.Category AS LatestAttachmentCategory
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                OUTER APPLY (
                    SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                 a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                    FROM DocumentAttachments a
                    WHERE a.DocumentId = d.DocumentId
                    ORDER BY a.UploadedDate DESC
                ) la
                WHERE (
                    d.Classification = 1
                    OR d.CreatedBy = @UserId
                    OR (d.Classification = 2 AND EXISTS (
                        SELECT 1 FROM DocumentPermissions dp
                        WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                    ))
                )
                ORDER BY d.Year ASC, d.DocumentNumber ASC";

            var documents = new List<Document>();

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                documents.Add(MapDocument(reader));

            return documents;
        }

        private async Task<int> GetCount(SqlConnection connection, string sql)
        {
            using var cmd = new SqlCommand(sql, connection);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<List<Document>> ReadDocuments(
            SqlConnection connection,
            string sql,
            int page,
            int pageSize)
        {
            var documents = new List<Document>();

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                documents.Add(MapDocument(reader));

            return documents;
        }

        private static Document MapDocument(SqlDataReader reader)
        {
            var document = DocumentMapper.MapToDocument(reader);

            document.Classification =
                (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

            if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
            {
                document.Creator = new Users
                {
                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                };
            }

            return document;
        }
    }
}