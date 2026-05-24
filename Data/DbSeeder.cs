using eProtokoll.Helpers;
using eProtokoll.Models;
using eProtokoll.Repositories.User;

namespace eProtokoll.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminUser(IUserRepository userRepository)
        {
            var existing = await userRepository.GetByUsernameAsync("admin");
            if (existing != null) return;

            var admin = new Users
            {
                UserName = "admin",
                PasswordHash = PasswordHelper.Hash("admin123"),
                Email = "admin@eprotokoll.al",
                FirstName = "Super",
                LastName = "Admin",
                Role = Users.UserRole.Administrator,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await userRepository.CreateAsync(admin);
        }
    }
}