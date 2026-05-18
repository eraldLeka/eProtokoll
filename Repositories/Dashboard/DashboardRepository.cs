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

        // ==================== SHARED ====================

        public async Task<DashboardStats> GetDocumentStatsAsync(int? userId = null)
        {
            var stats = new DashboardStats();

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
                SELECT
                    SUM(CASE WHEN DocumentType = 1 THEN 1 ELSE 0 END),
                    SUM(CASE WHEN DocumentType = 2 THEN 1 ELSE 0 END),
                    SUM(CASE WHEN DocumentType = 3 THEN 1 ELSE 0 END)
                FROM Documents d
                {whereClause}", connection);

            if (userId.HasValue)
                cmd.Parameters.AddWithValue("@UserId", userId.Value);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                stats.TotalIncoming = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                stats.TotalOutgoing = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                stats.TotalInternal = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            }

            return stats;
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
                    d.Subject,d.Classification, d.Priority,
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

        // ==================== ADMIN ====================

        public async Task<AdminStats> GetAdminStatsAsync()
        {
            var stats = new AdminStats();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            stats.TotalUsers = (int)(await new SqlCommand(
                "SELECT COUNT(*) FROM Users", connection).ExecuteScalarAsync())!;

            stats.TotalDocuments = (int)(await new SqlCommand(
                "SELECT COUNT(*) FROM Documents", connection).ExecuteScalarAsync())!;

            stats.DocumentsToday = (int)(await new SqlCommand(
                "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = CAST(GETDATE() AS DATE)",
                connection).ExecuteScalarAsync())!;

            stats.TotalInstitutions = (int)(await new SqlCommand(
                "SELECT COUNT(*) FROM Institutions WHERE IsActive = 1",
                connection).ExecuteScalarAsync())!;

            stats.UsersThisMonth = (int)(await new SqlCommand(
                @"SELECT COUNT(*) FROM Users
                  WHERE MONTH(CreatedDate) = MONTH(GETDATE())
                  AND YEAR(CreatedDate) = YEAR(GETDATE())",
                connection).ExecuteScalarAsync())!;

            stats.InstitutionsThisMonth = (int)(await new SqlCommand(
                @"SELECT COUNT(*) FROM Institutions
                  WHERE MONTH(CreatedDate) = MONTH(GETDATE())
                  AND YEAR(CreatedDate) = YEAR(GETDATE())",
                connection).ExecuteScalarAsync())!;

            using var cmd = new SqlCommand(
                "SELECT DocumentType, COUNT(*) FROM Documents GROUP BY DocumentType",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var docType = reader.GetInt32(0);
                var count = reader.GetInt32(1);

                if (docType == 1) stats.Incoming = count;
                else if (docType == 2) stats.Outgoing = count;
                else if (docType == 3) stats.Internal = count;
            }

            return stats;
        }

        public async Task<List<RecentActivity>> GetRecentActivityAsync()
        {
            var activities = new List<RecentActivity>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT TOP 5
                    d.CreatedDate,
                    u.FirstName + ' ' + u.LastName AS UserName,
                    CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) AS ProtocolNumber,
                    d.Subject,
                    d.DocumentType
                FROM Documents d
                INNER JOIN Users u ON d.CreatedBy = u.Id
                ORDER BY d.CreatedDate DESC", connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                activities.Add(new RecentActivity
                {
                    Time = reader.GetDateTime(0).ToString("HH:mm"),
                    UserName = reader.GetString(1),
                    ProtocolNumber = reader.GetString(2),
                    Subject = reader.GetString(3),
                    DocumentType = (DocumentType)reader.GetInt32(4)
                });
            }

            return activities;
        }

        public async Task<List<MonthlyCount>> GetMonthlyDataAsync()
        {
            var result = new List<MonthlyCount>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT
                    FORMAT(CreatedDate, 'MMM yyyy') AS MonthName,
                    COUNT(*) AS Total
                FROM Documents
                WHERE CreatedDate >= DATEADD(MONTH, -6, GETDATE())
                GROUP BY FORMAT(CreatedDate, 'MMM yyyy'), YEAR(CreatedDate), MONTH(CreatedDate)
                ORDER BY YEAR(CreatedDate), MONTH(CreatedDate)", connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new MonthlyCount
                {
                    MonthName = reader.GetString(0),
                    Count = reader.GetInt32(1)
                });
            }

            return result;
        }
    }
}