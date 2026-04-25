using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ================= CORE =================

        private async Task<int> CountAsync(SqlCommand cmd)
        {
            using var conn = new SqlConnection(_connectionString);
            cmd.Connection = conn;

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();

            return result == null ? 0 : Convert.ToInt32(result);
        }

        // ================= TOTALS =================

        public Task<int> GetTotalDocumentsAsync()
            => CountAsync(new SqlCommand("SELECT COUNT(*) FROM Documents"));

        public Task<int> GetTotalUsersAsync()
            => CountAsync(new SqlCommand("SELECT COUNT(*) FROM Users"));

        public Task<int> GetTotalInstitutionsAsync()
            => CountAsync(new SqlCommand("SELECT COUNT(*) FROM Institutions"));

        public Task<int> GetTotalByTypeAsync(DocumentType type)
        {
            var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM Documents 
                WHERE DocumentType = @Type");

            cmd.Parameters.AddWithValue("@Type", (int)type);

            return CountAsync(cmd);
        }

        public Task<int> GetTotalByPriorityAsync(Priority priority)
        {
            var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Documents
                WHERE Priority = @Priority");

            cmd.Parameters.AddWithValue("@Priority", (int)priority);

            return CountAsync(cmd);
        }

        // ================= TIME =================

        public Task<int> GetTodayDocumentsAsync()
        {
            var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM Documents 
                WHERE CAST(CreatedDate AS DATE) = @Today");

            cmd.Parameters.AddWithValue("@Today", DateTime.Now.Date);

            return CountAsync(cmd);
        }

        public Task<int> GetCurrentWeekDocumentsAsync()
        {
            var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM Documents 
                WHERE CreatedDate >= @WeekAgo");

            cmd.Parameters.AddWithValue("@WeekAgo", DateTime.Now.AddDays(-7));

            return CountAsync(cmd);
        }

        public Task<int> GetCurrentMonthDocumentsAsync()
        {
            var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM Documents
                WHERE MONTH(CreatedDate) = @Month
                  AND YEAR(CreatedDate) = @Year");

            cmd.Parameters.AddWithValue("@Month", DateTime.Now.Month);
            cmd.Parameters.AddWithValue("@Year", DateTime.Now.Year);

            return CountAsync(cmd);
        }

        // ================= TOP USERS =================

        public async Task<List<TopUser>> GetTopUsersAsync(int topCount)
        {
            var cmd = new SqlCommand(@"
                SELECT TOP (@TopCount)
                    u.Id,
                    CAST(u.FirstName + ' ' + u.LastName AS NVARCHAR(200)) AS FullName,
                    CAST(u.Role AS NVARCHAR(50)) AS Role,
                    COUNT(d.DocumentId) AS TotalDocuments
                FROM Users u
                LEFT JOIN Documents d ON d.CreatedBy = u.Id
                GROUP BY u.Id, u.FirstName, u.LastName, u.Role
                ORDER BY COUNT(d.DocumentId) DESC");

            cmd.Parameters.AddWithValue("@TopCount", topCount);

            using var conn = new SqlConnection(_connectionString);
            cmd.Connection = conn;

            await conn.OpenAsync();

            var list = new List<TopUser>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new TopUser
                {
                    UserId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Role = Enum.TryParse<Users.UserRole>(reader.GetString(2), out var r)
                        ? r
                        : Users.UserRole.Employee,
                    TotalDocuments = reader.GetInt32(3)
                });
            }

            return list;
        }

        // ================= TOP INSTITUTIONS =================

        public async Task<List<TopInstitution>> GetTopInstitutionsAsync(int topCount)
        {
            var cmd = new SqlCommand(@"
                SELECT TOP (@TopCount)
                    i.InstitutionId,
                    i.Name,
                    COUNT(d.DocumentId) AS TotalDocuments,
                    ISNULL(SUM(CASE WHEN d.DocumentType = @Incoming THEN 1 ELSE 0 END), 0) AS Incoming,
                    ISNULL(SUM(CASE WHEN d.DocumentType = @Outgoing THEN 1 ELSE 0 END), 0) AS Outgoing
                FROM Institutions i
                LEFT JOIN Documents d ON d.InstitutionId = i.InstitutionId
                GROUP BY i.InstitutionId, i.Name
                ORDER BY COUNT(d.DocumentId) DESC");

            cmd.Parameters.AddWithValue("@TopCount", topCount);
            cmd.Parameters.AddWithValue("@Incoming", (int)DocumentType.Incoming);
            cmd.Parameters.AddWithValue("@Outgoing", (int)DocumentType.Outgoing);

            using var conn = new SqlConnection(_connectionString);
            cmd.Connection = conn;

            await conn.OpenAsync();

            var list = new List<TopInstitution>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new TopInstitution
                {
                    InstitutionId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    TotalDocuments = reader.GetInt32(2),
                    Incoming = reader.GetInt32(3),
                    Outgoing = reader.GetInt32(4)
                });
            }

            return list;
        }
    }
}