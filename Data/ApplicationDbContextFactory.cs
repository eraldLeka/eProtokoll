using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eProtokoll.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(
                "Server=ERALD\\SQLEXPRESS;Database=eProtokollDB;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
            );
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}