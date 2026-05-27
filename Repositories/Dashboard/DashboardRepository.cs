using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories.Dashboard
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<Document>> GetRecentDocumentsAsync(int? userId = null)
        {
            var documents = new List<Document>();

            var whereClause = userId.HasValue
                ? @"WHERE (
                        d.Classification = 1
                        OR d.CreatedBy = @UserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                        ))
                    )"
                : "";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand($@"
                SELECT TOP 5
                    d.DocumentId, d.DocumentNumber, d.Year, d.DocumentType,
                    d.Subject, d.Classification, d.Priority,
                    d.HasAttachments, d.CreatedBy, d.CreatedDate,
                    d.RequiresResponse,
                    u.UserName AS CreatorUserName,
                    u.FirstName AS CreatorFirstName,
                    u.LastName AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {whereClause}
                ORDER BY d.CreatedDate DESC", connection);

            if (userId.HasValue)
                cmd.Parameters.AddWithValue("@UserId", userId.Value);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var document = DocumentMapper.MapToDocument(reader);

                if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
                {
                    document.Creator = new Users
                    {
                        UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                        FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                    };
                }

                documents.Add(document);
            }

            return documents;
        }

        public async Task<List<DailyCount>> GetDailyStatsAsync(int days = 7, int? userId = null)
        {
            var result = new Dictionary<string, int>();

            for (int i = days - 1; i >= 0; i--)
            {
                var day = DateTime.Now.AddDays(-i).ToString("dd/MM");
                result[day] = 0;
            }

            var whereClause = userId.HasValue
                ? @"AND (
                        d.Classification = 1
                        OR d.CreatedBy = @UserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                        ))
                    )"
                : "";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand($@"
                SELECT
                    CAST(d.CreatedDate AS DATE) AS Day,
                    COUNT(*) AS Total
                FROM Documents d
                WHERE d.CreatedDate >= DATEADD(DAY, -{days - 1}, CAST(GETDATE() AS DATE))
                {whereClause}
                GROUP BY CAST(d.CreatedDate AS DATE)
                ORDER BY Day", connection);

            if (userId.HasValue)
                cmd.Parameters.AddWithValue("@UserId", userId.Value);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var key = reader.GetDateTime(0).ToString("dd/MM");
                if (result.ContainsKey(key))
                    result[key] = reader.GetInt32(1);
            }

            return result.Select(kvp => new DailyCount
            {
                Date = kvp.Key,
                Count = kvp.Value
            }).ToList();
        }
    }
}