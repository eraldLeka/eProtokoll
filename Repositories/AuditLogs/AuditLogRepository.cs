using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories.AuditLogs
{
    

    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly string _connectionString;

        public AuditLogRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task LogAsync(AuditLog log)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO AuditLogs (UserId, UserName, Action, DocumentId, Description, Timestamp)
                VALUES (@UserId, @UserName, @Action, @DocumentId, @Description, @Timestamp)",
                connection);

            cmd.Parameters.AddWithValue("@UserId", log.UserId);
            cmd.Parameters.AddWithValue("@UserName", log.UserName);
            cmd.Parameters.AddWithValue("@Action", log.Action);
            cmd.Parameters.AddWithValue("@DocumentId", (object?)log.DocumentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)log.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Timestamp", log.Timestamp);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<AuditLog>> GetAllAsync()
        {
            var logs = new List<AuditLog>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(
                "SELECT * FROM AuditLogs ORDER BY Timestamp DESC",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new AuditLog
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Action = reader.GetString(reader.GetOrdinal("Action")),
                    DocumentId = reader.IsDBNull(reader.GetOrdinal("DocumentId"))
                        ? null : reader.GetInt32(reader.GetOrdinal("DocumentId")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? null : reader.GetString(reader.GetOrdinal("Description")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                });
            }

            return logs;
        }

        public async Task<List<AuditLog>> GetPagedAsync(int page, int pageSize)
        {
            var logs = new List<AuditLog>();
            var offset = (page - 1) * pageSize;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT * FROM AuditLogs
                ORDER BY Timestamp DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                connection);

            cmd.Parameters.AddWithValue("@Offset", offset);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new AuditLog
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Action = reader.GetString(reader.GetOrdinal("Action")),
                    DocumentId = reader.IsDBNull(reader.GetOrdinal("DocumentId"))
                        ? null : reader.GetInt32(reader.GetOrdinal("DocumentId")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? null : reader.GetString(reader.GetOrdinal("Description")),
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                });
            }

            return logs;
        }

        public async Task<int> CountAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM AuditLogs", connection);
            return (int)await cmd.ExecuteScalarAsync();
        }
    }
}