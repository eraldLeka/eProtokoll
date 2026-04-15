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

        // ── Admin & Manager: sheh të gjitha ──────────────────────────────────
        public async Task<(List<Document> Documents, int TotalItems)> GetPagedAsync(
            string searchTerm,
            int page,
            int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder("WHERE 1=1");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                where.Append(@"
                    AND (
                        CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) LIKE @Search
                        OR d.Subject LIKE @Search
                    )");
            }

            var totalItems = await GetCount(connection,
                $"SELECT COUNT(*) FROM Documents d {where}",
                searchTerm);

            var sql = $@"
                SELECT d.*,
                       u.UserName  AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName  AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {where}
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var documents = await ReadDocuments(connection, sql, searchTerm, page, pageSize);

            return (documents, totalItems);
        }

        // ── Employee: sheh vetëm dokumentet ku ka leje ───────────────────────
        public async Task<(List<Document> Documents, int TotalItems)> GetPagedForEmployeeAsync(
            string searchTerm,
            int page,
            int pageSize,
            int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder(@"
                WHERE EXISTS (
                    SELECT 1 FROM DocumentPermissions dp
                    WHERE dp.DocumentId = d.DocumentId
                      AND dp.UserId = @UserId
                )");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                where.Append(@"
                    AND (
                        CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) LIKE @Search
                        OR d.Subject LIKE @Search
                    )");
            }

            var countSql = $"SELECT COUNT(*) FROM Documents d {where}";
            int totalItems;

            using (var cmd = new SqlCommand(countSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

                totalItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var sql = $@"
                SELECT d.*,
                       u.UserName  AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName  AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {where}
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var documents = new List<Document>();

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    documents.Add(MapDocument(reader));
            }

            return (documents, totalItems);
        }

        // ── Print: Admin & Manager ────────────────────────────────────────────
        public async Task<List<Document>> GetForPrintAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT d.*,
                       u.UserName  AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName  AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                ORDER BY d.Year ASC, d.DocumentNumber ASC";

            var documents = new List<Document>();

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                documents.Add(MapDocument(reader));

            return documents;
        }

        // ── Print: Employee (vetëm dokumentet ku ka leje) ────────────────────
        public async Task<List<Document>> GetForPrintForEmployeeAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT d.*,
                       u.UserName  AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName  AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                WHERE EXISTS (
                    SELECT 1 FROM DocumentPermissions dp
                    WHERE dp.DocumentId = d.DocumentId
                      AND dp.UserId = @UserId
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

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<int> GetCount(SqlConnection connection, string sql, string searchTerm)
        {
            using var cmd = new SqlCommand(sql, connection);

            if (!string.IsNullOrEmpty(searchTerm))
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<List<Document>> ReadDocuments(
            SqlConnection connection,
            string sql,
            string searchTerm,
            int page,
            int pageSize)
        {
            var documents = new List<Document>();

            using var cmd = new SqlCommand(sql, connection);

            if (!string.IsNullOrEmpty(searchTerm))
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

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