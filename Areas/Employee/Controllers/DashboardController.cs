using Microsoft.AspNetCore.Mvc;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Now.Date;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // =========================
            // STATISTIKAT KRYESORE
            // =========================
            int totalIncoming = 0, totalOutgoing = 0, totalInternal = 0;

            using (var command = new SqlCommand(
                @"SELECT 
                    SUM(CASE WHEN Discriminator = 'IncomingDocument' THEN 1 ELSE 0 END),
                    SUM(CASE WHEN Discriminator = 'OutgoingDocument' THEN 1 ELSE 0 END),
                    SUM(CASE WHEN Discriminator = 'InternalDocument' THEN 1 ELSE 0 END)
                  FROM Documents d
                  WHERE
                    d.Classification = 1
                    OR d.CreatedBy = @UserId
                    OR (d.Classification = 2 AND EXISTS (
                        SELECT 1 FROM DocumentPermissions dp
                        WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                    ))", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    totalIncoming = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    totalOutgoing = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    totalInternal = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                }
            }

            ViewBag.TotalIncoming = totalIncoming;
            ViewBag.TotalOutgoing = totalOutgoing;
            ViewBag.TotalInternal = totalInternal;
            ViewBag.TotalDocuments = totalIncoming + totalOutgoing + totalInternal;

            // =========================
            // DOKUMENTET E FUNDIT
            // =========================
            var recentDocuments = new List<Document>();

            var queryRecent = @"
                SELECT TOP 10 d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                WHERE
                    d.Classification = 1
                    OR d.CreatedBy = @UserId
                    OR (d.Classification = 2 AND EXISTS (
                        SELECT 1 FROM DocumentPermissions dp
                        WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                    ))
                ORDER BY d.CreatedDate DESC";

            using (var command = new SqlCommand(queryRecent, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var document = DocumentMapper.MapToDocument(reader);

                    document.Classification =
                        (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

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

            // =========================
            // STATISTIKA 7 DITËT E FUNDIT
            // =========================
            var last7Days = Enumerable.Range(0, 7)
                                      .Select(i => today.AddDays(-i))
                                      .Reverse()
                                      .ToList();

            var dailyStats = new List<object>();

            foreach (var day in last7Days)
            {
                using var command = new SqlCommand(
                    @"SELECT COUNT(*) FROM Documents d
                      WHERE CAST(d.CreatedDate AS DATE) = @Day
                      AND (
                          d.Classification = 1
                          OR d.CreatedBy = @UserId
                          OR (d.Classification = 2 AND EXISTS (
                              SELECT 1 FROM DocumentPermissions dp
                              WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                          ))
                      )", connection);

                command.Parameters.AddWithValue("@Day", day.Date);
                command.Parameters.AddWithValue("@UserId", userId);

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