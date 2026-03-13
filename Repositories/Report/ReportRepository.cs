using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== HELPER ====================

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

        public Task<int> GetTotalByDiscriminatorAsync(string discriminator) =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE Discriminator = @Discriminator",
                cmd => cmd.Parameters.AddWithValue("@Discriminator", discriminator));

        // ==================== INSTITUCIONET ====================

        public Task<int> GetTotalInstitutionsAsync() =>
            CountAsync("SELECT COUNT(*) FROM Institutions");

        public Task<int> GetActiveInstitutionsAsync() =>
            CountAsync("SELECT COUNT(*) FROM Institutions WHERE IsActive = 1");

        // ==================== PRIORITETI ====================

        public Task<int> GetTotalByPriorityAsync(Priority priority) =>
            CountAsync(
                "SELECT COUNT(*) FROM Documents WHERE Priority = @Priority",
                cmd => cmd.Parameters.AddWithValue("@Priority", (int)priority));

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

        // ==================== TRACKING STATISTIKA ====================

        public Task<int> GetActiveTrackingsAsync() =>
            CountAsync("SELECT COUNT(*) FROM DocumentTrackings WHERE IsActive = 1 AND CompletedDate IS NULL");

        public Task<int> GetCompletedTrackingsAsync() =>
            CountAsync("SELECT COUNT(*) FROM DocumentTrackings WHERE CompletedDate IS NOT NULL");

        // ==================== AUDIT LOG ====================

        public async Task<List<DocumentTracking>> GetAuditLogAsync()
        {
            var auditLogs = new List<DocumentTracking>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT
                    dt.TrackingId, dt.DocumentId, dt.AssignedToUserId, dt.AssignedByUserId,
                    dt.AssignedDate, dt.Priority, dt.DueDate, dt.Notes,
                    dt.CompletedDate, dt.IsActive, dt.CreatedDate,
                    d.ProtocolNumber  AS DocumentProtocolNumber,
                    d.Subject         AS DocumentSubject,
                    d.Discriminator   AS DocumentDiscriminator,
                    u1.FirstName      AS AssignedToFirstName,
                    u1.LastName       AS AssignedToLastName,
                    u2.FirstName      AS AssignedByFirstName,
                    u2.LastName       AS AssignedByLastName
                FROM DocumentTrackings dt
                LEFT JOIN Documents d  ON dt.DocumentId       = d.DocumentId
                LEFT JOIN Users u1     ON dt.AssignedToUserId = u1.Id
                LEFT JOIN Users u2     ON dt.AssignedByUserId = u2.Id
                ORDER BY dt.CreatedDate DESC", connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                auditLogs.Add(new DocumentTracking
                {
                    TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                    DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                    AssignedToUserId = reader.GetInt32(reader.GetOrdinal("AssignedToUserId")),
                    AssignedByUserId = reader.GetInt32(reader.GetOrdinal("AssignedByUserId")),
                    AssignedDate = reader.GetDateTime(reader.GetOrdinal("AssignedDate")),
                    Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate"))
                                           ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                                           ? null : reader.GetString(reader.GetOrdinal("Notes")),
                    CompletedDate = reader.IsDBNull(reader.GetOrdinal("CompletedDate"))
                                           ? null : reader.GetDateTime(reader.GetOrdinal("CompletedDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),

                    // NotMapped
                    DocumentProtocolNumber = reader.IsDBNull(reader.GetOrdinal("DocumentProtocolNumber"))
                                           ? null : reader.GetString(reader.GetOrdinal("DocumentProtocolNumber")),
                    DocumentSubject = reader.IsDBNull(reader.GetOrdinal("DocumentSubject"))
                                           ? null : reader.GetString(reader.GetOrdinal("DocumentSubject")),
                    DocumentDiscriminator = reader.IsDBNull(reader.GetOrdinal("DocumentDiscriminator"))
                                           ? null : reader.GetString(reader.GetOrdinal("DocumentDiscriminator")),

                    AssignedToUser = new Users
                    {
                        FirstName = reader.IsDBNull(reader.GetOrdinal("AssignedToFirstName"))
                                        ? string.Empty : reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                        LastName = reader.IsDBNull(reader.GetOrdinal("AssignedToLastName"))
                                        ? string.Empty : reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                    },
                    AssignedByUser = new Users
                    {
                        FirstName = reader.IsDBNull(reader.GetOrdinal("AssignedByFirstName"))
                                        ? string.Empty : reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                        LastName = reader.IsDBNull(reader.GetOrdinal("AssignedByLastName"))
                                        ? string.Empty : reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                    }
                });
            }

            return auditLogs;
        }
    }
}