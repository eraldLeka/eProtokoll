using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AlbanianMonths =
        {
            "Janar", "Shkurt", "Mars", "Prill", "Maj", "Qershor",
            "Korrik", "Gusht", "Shtator", "Tetor", "Nëntor", "Dhjetor"
        };

        public ReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        private async Task<int> CountAsync(string query, Action<SqlCommand>? addParams = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(query, connection);
            addParams?.Invoke(cmd);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // ==================== DOKUMENTET ====================
        public Task<int> GetTotalDocumentsAsync() =>
            CountAsync("SELECT COUNT(*) FROM Documents");

        public Task<int> GetTotalByTypeAsync(DocumentType type) =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Type",
                cmd => cmd.Parameters.AddWithValue("@Type", (int)type));

        // ==================== INSTITUCIONET ====================
        public Task<int> GetTotalInstitutionsAsync() =>
            CountAsync("SELECT COUNT(*) FROM Institutions");

        // ==================== KOHORE ====================
        public Task<int> GetCurrentMonthDocumentsAsync() =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE MONTH(CreatedDate) = @Month AND YEAR(CreatedDate) = @Year",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Month", DateTime.Now.Month);
                    cmd.Parameters.AddWithValue("@Year", DateTime.Now.Year);
                });

        public Task<int> GetCurrentWeekDocumentsAsync() =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @WeekAgo",
                cmd => cmd.Parameters.AddWithValue("@WeekAgo", DateTime.Now.AddDays(-7)));

        public Task<int> GetTodayDocumentsAsync() =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today",
                cmd => cmd.Parameters.AddWithValue("@Today", DateTime.Now.Date));

        // ==================== PRIORITETI ====================
        public Task<int> GetByPriorityAsync(Priority priority) =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE Priority = @Priority",
                cmd => cmd.Parameters.AddWithValue("@Priority", (int)priority));

        // ==================== GRAFIKU 12 MUAJ ====================
        public async Task<List<MonthlyDocumentCount>> GetMonthlyDocumentCountsAsync(int year)
        {
            var results = new Dictionary<int, int>();
            for (int i = 1; i <= 12; i++) results[i] = 0;

            const string sql = @"
                SELECT MONTH(CreatedDate) AS Month, COUNT(*) AS Total
                FROM Documents
                WHERE YEAR(CreatedDate) = @Year
                GROUP BY MONTH(CreatedDate)";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Year", year);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results[reader.GetInt32(0)] = reader.GetInt32(1);

            return results.Select(kvp => new MonthlyDocumentCount
            {
                Month = kvp.Key,
                MonthName = AlbanianMonths[kvp.Key - 1],
                Count = kvp.Value
            }).ToList();
        }

        // ==================== TOP 5 INSTITUCIONET ====================
        public async Task<List<TopInstitution>> GetTopInstitutionsAsync(int topCount = 5)
        {
            const string sql = @"
                SELECT TOP (@TopCount)
                    i.InstitutionId,
                    i.Name,
                    COUNT(d.DocumentId) AS TotalDocuments,
                    SUM(CASE WHEN d.DocumentType = @Incoming THEN 1 ELSE 0 END) AS Incoming,
                    SUM(CASE WHEN d.DocumentType = @Outgoing THEN 1 ELSE 0 END) AS Outgoing
                FROM Institutions i
                INNER JOIN Documents d ON d.InstitutionId = i.InstitutionId
                WHERE d.DocumentType IN (@Incoming, @Outgoing)
                GROUP BY i.InstitutionId, i.Name
                ORDER BY TotalDocuments DESC";

            var list = new List<TopInstitution>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@TopCount", topCount);
            cmd.Parameters.AddWithValue("@Incoming", (int)DocumentType.Incoming);
            cmd.Parameters.AddWithValue("@Outgoing", (int)DocumentType.Outgoing);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TopInstitution
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    TotalDocuments = reader.GetInt32(2),
                    Incoming = reader.GetInt32(3),
                    Outgoing = reader.GetInt32(4)
                });
            }

            return list;
        }

        // ==================== TOP 5 PËRDORUESIT ====================
        public async Task<List<TopUser>> GetTopUsersAsync(int topCount = 5)
        {
            const string sql = @"
                SELECT TOP (@TopCount)
                    u.Id,
                    ISNULL(u.FirstName + ' ' + u.LastName, 'N/A') AS FullName,
                    CAST(u.Role AS NVARCHAR(50)) AS RoleName,
                    COUNT(d.DocumentId) AS TotalDocuments
                FROM Users u
                INNER JOIN Documents d ON d.CreatedBy = u.Id
                GROUP BY u.Id, u.FirstName, u.LastName, u.Role
                ORDER BY TotalDocuments DESC";

            var list = new List<TopUser>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@TopCount", topCount);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TopUser
                {
                    UserId = reader.GetInt32(0),
                    FullName = reader.IsDBNull(1) ? "N/A" : reader.GetString(1).Trim(),
                    Role = reader.IsDBNull(2) ? "—" : reader.GetString(2),
                    TotalDocuments = reader.GetInt32(3)
                });
            }

            return list;
        }
    }
}