using Microsoft.AspNetCore.Mvc;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using eProtokoll.Services.Mappers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
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
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // === STATISTIKA BAZË ===
                ViewBag.TotalDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents");
                ViewBag.TotalIncoming = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1");
                ViewBag.TotalOutgoing = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2");
                ViewBag.TotalInternal = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3");

                // Dokumentet e sotme
                var queryToday = "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@Today", today);
                    ViewBag.TodayDocuments = (int)await command.ExecuteScalarAsync();
                }

                // Dokumentet e muajit
                var queryMonth = "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @ThisMonth";
                using (var command = new SqlCommand(queryMonth, connection))
                {
                    command.Parameters.AddWithValue("@ThisMonth", thisMonth);
                    ViewBag.MonthDocuments = (int)await command.ExecuteScalarAsync();
                }

                // === PRIORITETET ===
                var queryUrgent = @"SELECT COUNT(*) FROM Documents 
                                    WHERE Priority = @Priority AND Status != @Status";
                using (var command = new SqlCommand(queryUrgent, connection))
                {
                    command.Parameters.AddWithValue("@Priority", (int)Priority.Urgent);
                    command.Parameters.AddWithValue("@Status", (int)DocumentStatus.Completed);
                    ViewBag.UrgentDocuments = (int)await command.ExecuteScalarAsync();
                }

                // === STATUSET ===
                var queryInProgress = "SELECT COUNT(*) FROM Documents WHERE Status = @Status";
                using (var command = new SqlCommand(queryInProgress, connection))
                {
                    command.Parameters.AddWithValue("@Status", (int)DocumentStatus.InProgress);
                    ViewBag.InProgressDocuments = (int)await command.ExecuteScalarAsync();
                }

                var queryCompleted = "SELECT COUNT(*) FROM Documents WHERE Status = @Status";
                using (var command = new SqlCommand(queryCompleted, connection))
                {
                    command.Parameters.AddWithValue("@Status", (int)DocumentStatus.Completed);
                    ViewBag.CompletedDocuments = (int)await command.ExecuteScalarAsync();
                }

                // === DOKUMENTET E FUNDIT ===
                var recentDocuments = new List<Document>();
                var queryRecent = @"SELECT TOP 10 d.*, 
                                    c.Name as ClassificationName, c.ColorCode,
                                    u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName
                                    FROM Documents d
                                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                                    ORDER BY d.CreatedDate DESC";

                using (var command = new SqlCommand(queryRecent, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var document = DocumentMapper.MapToDocument(reader);

                            // Populate Classification
                            if (!reader.IsDBNull(reader.GetOrdinal("ClassificationName")))
                            {
                                document.Classification = new Classification
                                {
                                    ClassificationId = document.ClassificationId,
                                    Name = reader.GetString(reader.GetOrdinal("ClassificationName")),
                                    ColorCode = reader.IsDBNull(reader.GetOrdinal("ColorCode"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ColorCode"))
                                };
                            }

                            // Populate Creator
                            if (!reader.IsDBNull(reader.GetOrdinal("CreatorUserName")))
                            {
                                document.Creator = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                                };
                            }

                            recentDocuments.Add(document);
                        }
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
                    var queryDaily = "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Day";
                    using (var command = new SqlCommand(queryDaily, connection))
                    {
                        command.Parameters.AddWithValue("@Day", day.Date);
                        var count = (int)await command.ExecuteScalarAsync();

                        dailyStats.Add(new
                        {
                            Date = day.ToString("dd/MM"),
                            Count = count
                        });
                    }
                }
                ViewBag.DailyStats = dailyStats;

                // === DOKUMENTET PËR MUAJIN ===
                ViewBag.MonthlyIncoming = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1 AND CreatedDate >= @ThisMonth");
                ViewBag.MonthlyOutgoing = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2 AND CreatedDate >= @ThisMonth");
                ViewBag.MonthlyInternal = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3 AND CreatedDate >= @ThisMonth");
            }

            return View();
        }

        // Helper method për COUNT queries
        private async Task<int> ExecuteCountQuery(SqlConnection connection, string query)
        {
            using (var command = new SqlCommand(query, connection))
            {
                // Nëse query ka parameter @ThisMonth, e trajtojmë
                if (query.Contains("@ThisMonth"))
                {
                    var param = query.Contains("DocumentType = 1") || query.Contains("DocumentType = 2") || query.Contains("DocumentType = 3")
                                ? DateTime.Now.Date.AddMonths(0)  // Do ta mbulojmë me të dhënat e muajit
                                : DateTime.Now.Date;
                    command.Parameters.AddWithValue("@ThisMonth", new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));
                }

                var result = await command.ExecuteScalarAsync();
                return result != null ? (int)result : 0;
            }
        }
    }
}
