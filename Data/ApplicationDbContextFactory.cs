using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eProtokoll.Data
{
    /// <summary>
    /// Factory për krijimin e DbContext gjatë design-time migrations
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Connection string për design-time migrations
            optionsBuilder.UseSqlServer(
                "Server=ERALD\\SQLEXPRESS;Database=eProtokollDB;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
            );

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}