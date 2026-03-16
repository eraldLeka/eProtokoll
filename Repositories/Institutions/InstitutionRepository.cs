using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories.Institutions
{
    public class InstitutionRepository : IInstitutionRepository
    {
        private readonly string _connectionString;

        public InstitutionRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IEnumerable<Institution>> GetAllAsync()
        {
            var institutions = new List<Institution>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Institutions ORDER BY Name", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                institutions.Add(InstitutionMapper.Map(reader));

            return institutions;
        }

        public async Task<Institution?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Institutions WHERE InstitutionId = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return InstitutionMapper.Map(reader);

            return null;
        }

        public async Task CreateAsync(Institution institution)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                INSERT INTO Institutions 
                    (Name, ShortName, Type, Adress, PostalCode, Country, Phone, Email, Website,
                     ContactPerson, ContactPosition, ContactEmail, CreatedDate, CreatedBy)
                VALUES 
                    (@Name, @ShortName, @Type, @Adress, @PostalCode, @Country, @Phone, @Email, @Website,
                     @ContactPerson, @ContactPosition, @ContactEmail, @CreatedDate, @CreatedBy)",
                connection);

            command.Parameters.AddWithValue("@Name", institution.Name);
            command.Parameters.AddWithValue("@ShortName", (object?)institution.ShortName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Type", (int)institution.Type);
            command.Parameters.AddWithValue("@Adress", (object?)institution.Adress ?? DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", (object?)institution.PostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Country", (object?)institution.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)institution.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)institution.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@Website", (object?)institution.Website ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactPerson", (object?)institution.ContactPerson ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactPosition", (object?)institution.ContactPosition ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactEmail", (object?)institution.ContactEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", institution.CreatedDate);
            command.Parameters.AddWithValue("@CreatedBy", (object?)institution.CreatedBy ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(Institution institution)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                UPDATE Institutions SET
                    Name = @Name,
                    ShortName = @ShortName,
                    Type = @Type,
                    Adress = @Adress,
                    PostalCode = @PostalCode,
                    Country = @Country,
                    Phone = @Phone,
                    Email = @Email,
                    Website = @Website,
                    ContactPerson = @ContactPerson,
                    ContactPosition = @ContactPosition,
                    ContactEmail = @ContactEmail,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                WHERE InstitutionId = @InstitutionId",
                connection);

            command.Parameters.AddWithValue("@InstitutionId", institution.InstitutionId);
            command.Parameters.AddWithValue("@Name", institution.Name);
            command.Parameters.AddWithValue("@ShortName", (object?)institution.ShortName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Type", (int)institution.Type);
            command.Parameters.AddWithValue("@Adress", (object?)institution.Adress ?? DBNull.Value);
            command.Parameters.AddWithValue("@PostalCode", (object?)institution.PostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Country", (object?)institution.Country ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)institution.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)institution.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@Website", (object?)institution.Website ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactPerson", (object?)institution.ContactPerson ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactPosition", (object?)institution.ContactPosition ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactEmail", (object?)institution.ContactEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedDate", institution.ModifiedDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedBy", (object?)institution.ModifiedBy ?? DBNull.Value);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("DELETE FROM Institutions WHERE InstitutionId = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> GetDocumentCountAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT COUNT(*) FROM Documents
                WHERE InstitutionId = @Id AND (DocumentType = 1 OR DocumentType = 2)",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }
    }
}