using eProtokoll.Models;
using Microsoft.AspNetCore.Identity;

namespace eProtokoll.Data
{
    public static class DbSeeder
    {
        // ===================== SEED ROLES =====================

        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Administrator", "Manager", "Employee" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        // ===================== SEED ADMIN USER =====================

        public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
        {
            var admin = await userManager.FindByNameAsync("admin");

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@eprotokoll.al",
                    FirstName = "Super",
                    LastName = "Admin",
                    Role = ApplicationUser.UserRole.Administrator, // business role
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin@2025");

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Admin user could not be created: " +
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                }
            }

            // ===================== IDENTITY ROLE LINK (KRITIKE) =====================

            if (!await userManager.IsInRoleAsync(admin, "Administrator"))
            {
                await userManager.AddToRoleAsync(admin, "Administrator");
            }
        }
    }
}
