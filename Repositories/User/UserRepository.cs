using eProtokoll.Models;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Repositories.User
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Merr userin sipas username
        public async Task<Users?> GetByUsernameAsync(string username)
        {
            using var connection= new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                "SELECT * FROM Users WHERE UserName = @username", connection);
            cmd.Parameters.AddWithValue("@username", username);

            await connection.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapUser(reader);

            return null;
        }

        // Merr userin sipas Id
        public async Task<Users?> GetByIdAsync(int id)
        {
            using var connection= new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                "SELECT * FROM Users WHERE Id = @id", connection);
            cmd.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return MapUser(reader);

            return null;
        }

        // Merr të gjithë users
        public async Task<IEnumerable<Users>> GetAllAsync()
        {
            var users = new List<Users>();

            using var connection= new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                "SELECT * FROM Users ORDER BY FirstName", connection);

            await connection.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                users.Add(MapUser(reader));

            return users;
        }

        // Krijo user të ri
        public async Task CreateAsync(Users user)
        {
            using var connection= new SqlConnection(_connectionString);
            var cmd = new SqlCommand(@"
                INSERT INTO Users 
                    (UserName, PasswordHash, FirstName, LastName, Email, 
                     PhoneNumber, Position, Department, Role, IsActive, CreatedDate)
                VALUES 
                    (@username, @passwordHash, @firstName, @lastName, @email,
                     @phoneNumber, @position, @department, @role, @isActive, @createdDate)",
                 connection);

            cmd.Parameters.AddWithValue("@username", user.UserName);
            cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@firstName", user.FirstName);
            cmd.Parameters.AddWithValue("@lastName", user.LastName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@phoneNumber", (object?)user.PhoneNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@position", (object?)user.Position ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@department", (object?)user.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@role", (int)user.Role);
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);
            cmd.Parameters.AddWithValue("@createdDate", user.CreatedDate);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Modifiko user
        public async Task UpdateAsync(Users user)
        {
            using var connection= new SqlConnection(_connectionString);
            var cmd = new SqlCommand(@"
                UPDATE Users SET
                    FirstName = @firstName,
                    LastName = @lastName,
                    Email = @email,
                    PhoneNumber = @phoneNumber,
                    Position = @position,
                    Department = @department,
                    Role = @role,
                    IsActive = @isActive,
                    ModifiedDate = @modifiedDate,
                    ModifiedBy = @modifiedBy
                WHERE Id = @id",
                 connection);

            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.Parameters.AddWithValue("@firstName", user.FirstName);
            cmd.Parameters.AddWithValue("@lastName", user.LastName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@phoneNumber", (object?)user.PhoneNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@position", (object?)user.Position ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@department", (object?)user.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@role", (int)user.Role);
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);
            cmd.Parameters.AddWithValue("@modifiedDate", (object?)user.ModifiedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@modifiedBy", (object?)user.ModifiedBy ?? DBNull.Value);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Fshij user
        public async Task DeleteAsync(int id)
        {
            using var connection= new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                "DELETE FROM Users WHERE Id = @id", connection);
            cmd.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Map reader → ApplicationUser
        private static Users MapUser(SqlDataReader reader)
        {
            return new Users
            {
                Id = (int)reader["Id"],
                UserName = (string)reader["UserName"],
                PasswordHash = (string)reader["PasswordHash"],
                FirstName = (string)reader["FirstName"],
                LastName = (string)reader["LastName"],
                Email = (string)reader["Email"],
                PhoneNumber = reader["PhoneNumber"] as string,
                Position = reader["Position"] as string,
                Department = reader["Department"] as string,
                Role = (Users.UserRole)(int)reader["Role"],
                IsActive = (bool)reader["IsActive"],
                CreatedDate = (DateTime)reader["CreatedDate"],
                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                ModifiedBy = reader["ModifiedBy"] as string
            };
        }
    }
}
