using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Services.ProtocolNumber
{
    public class ProtocolNumberService : IProtocolNumberService
    {
        private readonly string _connectionString;

        public ProtocolNumberService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<int> GetNextDocumentNumberAsync(DocumentType type, int year)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT ISNULL(MAX(DocumentNumber), 0) + 1
                FROM Documents
                WHERE Year = @Year";

            using var cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Year", year);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}