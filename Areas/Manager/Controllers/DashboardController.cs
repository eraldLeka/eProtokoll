using Microsoft.AspNetCore.Mvc;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // === DOKUMENTET E FUNDIT ===
            var recentDocuments = new List<Document>();

            var queryRecent = @"
                SELECT TOP 10 d.*,
                       u.UserName as CreatorUserName,
                       u.FirstName as CreatorFirstName,
                       u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                ORDER BY d.CreatedDate DESC";

            using (var command = new SqlCommand(queryRecent, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var document = DocumentMapper.MapToDocument(reader);

                    // Classification enum
                    document.Classification =
                        (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

                    // Creator
                    if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
                    {
                        document.Creator = new Users
                        {
                            UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                            FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                        };
                    }

                    recentDocuments.Add(document);
                }
            }

            ViewBag.RecentDocuments = recentDocuments;

            // === AKTIVITETI I 7 DITËVE ===
            var last7Days = Enumerable.Range(0, 7)
                                       .Select(i => today.AddDays(-i))
                                       .Reverse()
                                       .ToList();

            var dailyStats = new List<object>();

            foreach (var day in last7Days)
            {
                using var command = new SqlCommand(
                    "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Day",
                    connection);

                command.Parameters.AddWithValue("@Day", day.Date);

                var count = (int)await command.ExecuteScalarAsync();

                dailyStats.Add(new
                {
                    Date = day.ToString("dd/MM"),
                    Count = count
                });
            }

            ViewBag.DailyStats = dailyStats;

            return View();
        }
    }
}