using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using System.Text;

namespace eProtokoll.Repositories
{
    public class TrackingRepository : ITrackingRepository
    {
        private readonly string _connectionString;

        public TrackingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ==================== SHARED QUERY ====================

        private const string SelectColumns = @"
            SELECT 
                dt.TrackingId, dt.DocumentId, dt.AssignedToUserId, dt.AssignedByUserId,
                dt.AssignedDate, dt.Priority, dt.DueDate, dt.Notes,
                dt.CompletedDate, dt.CreatedDate,
                d.DocumentType,
                CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) AS DocumentProtocolNumber,
                d.Subject AS DocumentSubject,
                uat.FirstName AS AssignedToFirstName,
                uat.LastName AS AssignedToLastName,
                uab.FirstName AS AssignedByFirstName,
                uab.LastName AS AssignedByLastName
            FROM DocumentTrackings dt
            LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
            LEFT JOIN Users uat ON dt.AssignedToUserId = uat.Id
            LEFT JOIN Users uab ON dt.AssignedByUserId = uab.Id";

        // ==================== INDEX — MANAGER ====================

        public async Task<(List<DocumentTracking> Trackings, int TotalCount)> GetAllAsync(
            int page, int pageSize, string searchTerm = "")
        {
            var trackings = new List<DocumentTracking>();
            int totalCount = 0;

            var where = new StringBuilder(" WHERE 1=1");
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                where.Append(@" AND (
                    CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) LIKE @SearchTerm
                    OR d.Subject LIKE @SearchTerm
                    OR dt.Notes LIKE @SearchTerm)");

                parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(
                $"SELECT COUNT(*) FROM DocumentTrackings dt LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId {where}",
                connection))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.ParameterName, p.Value);

                totalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var query = $@"{SelectColumns}
                {where}
                ORDER BY dt.AssignedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, connection))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.ParameterName, p.Value);

                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    trackings.Add(MapTracking(reader));
            }

            return (trackings, totalCount);
        }

        // ==================== INDEX — EMPLOYEE ====================

        public async Task<(List<DocumentTracking> Trackings, int TotalCount)> GetByUserAsync(
            int page, int pageSize, int userId)
        {
            var trackings = new List<DocumentTracking>();
            int totalCount = 0;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM DocumentTrackings dt
                WHERE dt.AssignedToUserId = @UserId
                AND dt.CompletedDate IS NULL", connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                totalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var query = $@"{SelectColumns}
                WHERE dt.AssignedToUserId = @UserId
                AND dt.CompletedDate IS NULL
                ORDER BY dt.AssignedDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    trackings.Add(MapTracking(reader));
            }

            return (trackings, totalCount);
        }

        // ==================== DETAILS ====================

        public async Task<DocumentTracking?> GetByIdAsync(int trackingId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = $@"{SelectColumns}
                WHERE dt.TrackingId = @TrackingId";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TrackingId", trackingId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return MapTracking(reader);
        }

        // ==================== INSERT ====================

        public async Task InsertAsync(DocumentTracking model, int assignedByUserId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                INSERT INTO DocumentTrackings (
                    DocumentId, AssignedToUserId, AssignedByUserId,
                    AssignedDate, Priority, DueDate, Notes, IsActive, CreatedDate
                ) VALUES (
                    @DocumentId, @AssignedToUserId, @AssignedByUserId,
                    @AssignedDate, @Priority, @DueDate, @Notes, @IsActive, @CreatedDate
                )", connection);

            cmd.Parameters.AddWithValue("@DocumentId", model.DocumentId);
            cmd.Parameters.AddWithValue("@AssignedToUserId", model.AssignedToUserId);
            cmd.Parameters.AddWithValue("@AssignedByUserId", assignedByUserId);
            cmd.Parameters.AddWithValue("@AssignedDate", model.AssignedDate);
            cmd.Parameters.AddWithValue("@Priority", (int)model.Priority);
            cmd.Parameters.AddWithValue("@DueDate", (object?)model.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", true);
            cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }

        // ==================== COMPLETE ====================

        public async Task CompleteAsync(int trackingId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE DocumentTrackings
                SET CompletedDate = @CompletedDate
                WHERE TrackingId = @TrackingId", connection);

            cmd.Parameters.AddWithValue("@TrackingId", trackingId);
            cmd.Parameters.AddWithValue("@CompletedDate", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }

        // ==================== NOTIFICATIONS ====================

        public async Task<int> GetNewAssignedCountAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM DocumentTrackings dt
                WHERE dt.AssignedToUserId = @UserId
                  AND dt.CompletedDate IS NULL
                  AND CAST(dt.AssignedDate AS DATE) = CAST(GETDATE() AS DATE)", connection);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<int> GetOverdueCountAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM DocumentTrackings dt
                WHERE dt.AssignedToUserId = @UserId
                  AND dt.CompletedDate IS NULL
                  AND dt.DueDate IS NOT NULL
                  AND dt.DueDate < GETDATE()", connection);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<DocumentTracking>> GetNotificationItemsAsync(int userId, int take = 10)
        {
            var trackings = new List<DocumentTracking>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = $@"{SelectColumns}
                WHERE dt.AssignedToUserId = @UserId
                AND dt.CompletedDate IS NULL
                ORDER BY
                    CASE
                        WHEN dt.DueDate IS NOT NULL
                         AND dt.DueDate < GETDATE()
                        THEN 0 ELSE 1
                    END,
                    dt.AssignedDate DESC
                OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Take", take);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                trackings.Add(MapTracking(reader));

            return trackings;
        }

        // ==================== DROPDOWNS ====================

        public async Task<List<Document>> GetDocumentsForDropdownAsync()
        {
            var documents = new List<Document>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT DocumentId, DocumentNumber, Year, Subject, DocumentType
                FROM Documents d
                WHERE d.DocumentType IN (@IncomingType, @InternalType)
                AND d.RequiresResponse = 1
                AND NOT EXISTS (
                    SELECT 1
                    FROM DocumentTrackings dt
                    WHERE dt.DocumentId = d.DocumentId
                    AND dt.CompletedDate IS NULL
                )
                ORDER BY d.CreatedDate DESC", connection);

            cmd.Parameters.AddWithValue("@IncomingType", (int)DocumentType.Incoming);
            cmd.Parameters.AddWithValue("@InternalType", (int)DocumentType.Internal);

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
                        ? ""
                        : reader.GetString(reader.GetOrdinal("Subject"))
                });
            }

            return documents;
        }

        public async Task<List<Users>> GetEmployeesAsync()
        {
            var employees = new List<Users>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT Id, FirstName, LastName
                FROM Users
                WHERE Role = 3 AND IsActive = 1
                ORDER BY FirstName, LastName", connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                employees.Add(new Users
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName"))
                });
            }

            return employees;
        }

        // ==================== MAPPER ====================

        private static DocumentTracking MapTracking(SqlDataReader reader)
        {
            return new DocumentTracking
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
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                DocumentType = reader.IsDBNull(reader.GetOrdinal("DocumentType"))
                    ? default
                    : (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),

                DocumentProtocolNumber = reader.IsDBNull(reader.GetOrdinal("DocumentProtocolNumber"))
                    ? null : reader.GetString(reader.GetOrdinal("DocumentProtocolNumber")),

                DocumentSubject = reader.IsDBNull(reader.GetOrdinal("DocumentSubject"))
                    ? null : reader.GetString(reader.GetOrdinal("DocumentSubject")),

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
            };
        }
    }
}