using eProtokoll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class ReportController : Controller
    {
        private readonly string _connectionString;

        public ReportController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Manager/Report
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Total counts
                ViewBag.TotalDocuments = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM Documents");
                ViewBag.TotalIncoming = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Incoming",
                    new SqlParameter("@Incoming", (int)DocumentType.Incoming));
                ViewBag.TotalOutgoing = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Outgoing",
                    new SqlParameter("@Outgoing", (int)DocumentType.Outgoing));
                ViewBag.TotalInternal = await ExecuteScalarIntAsync(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Internal",
                    new SqlParameter("@Internal", (int)DocumentType.Internal));

                // By period
                ViewBag.TodayCount = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today",
                    new SqlParameter("@Today", today));
                ViewBag.MonthCount = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @MonthStart",
                    new SqlParameter("@MonthStart", thisMonth));
                ViewBag.YearCount = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @YearStart",
                    new SqlParameter("@YearStart", thisYear));

                // By status (models: Registered = 1, InProgress = 2, Completed = 3)
                ViewBag.Draft = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Status = @Registered",
                    new SqlParameter("@Registered", (int)DocumentStatus.Registered));
                ViewBag.InProgress = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Status = @InProgress",
                    new SqlParameter("@InProgress", (int)DocumentStatus.InProgress));
                ViewBag.Completed = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Status = @Completed",
                    new SqlParameter("@Completed", (int)DocumentStatus.Completed));
                ViewBag.Archived = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE IsArchived = 1");

                // By priority
                ViewBag.Urgent = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Priority = @Urgent",
                    new SqlParameter("@Urgent", (int)Priority.Urgent));
                ViewBag.High = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Priority = @High",
                    new SqlParameter("@High", (int)Priority.High));
                ViewBag.Normal = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Priority = @Normal",
                    new SqlParameter("@Normal", (int)Priority.Normal));
                ViewBag.Low = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE Priority = @Low",
                    new SqlParameter("@Low", (int)Priority.Low));

                // Monthly data for chart (last 12 months)
                var monthlyData = new List<object>();
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = today.AddMonths(-i);
                    monthStart = new DateTime(monthStart.Year, monthStart.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    var count = await ExecuteScalarIntAsync(connection,
                        "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @MonthStart AND CreatedDate < @MonthEnd",
                        new SqlParameter("@MonthStart", monthStart),
                        new SqlParameter("@MonthEnd", monthEnd));

                    monthlyData.Add(new
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        Count = count
                    });
                }
                ViewBag.MonthlyData = monthlyData;

                // Top institutions (based on Incoming documents in the current year)
                var topInstitutions = new List<object>();
                var topInstQuery = @"
                    SELECT TOP 10 d.InstitutionId, ISNULL(i.Name, 'N/A') AS Name, COUNT(*) AS Cnt
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    WHERE d.DocumentType = @Incoming AND d.CreatedDate >= @YearStart
                    GROUP BY d.InstitutionId, i.Name
                    ORDER BY Cnt DESC";
                using (var cmd = new SqlCommand(topInstQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Incoming", (int)DocumentType.Incoming);
                    cmd.Parameters.AddWithValue("@YearStart", thisYear);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            topInstitutions.Add(new
                            {
                                Name = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                                Count = reader.GetInt32(2)
                            });
                        }
                    }
                }
                ViewBag.TopInstitutions = topInstitutions;

                // Top users (who created documents this year)
                var topUsersList = new List<object>();
                var topUsersQuery = @"
                    SELECT TOP 10 d.CreatedBy, ISNULL(u.FirstName + ' ' + u.LastName, 'N/A') AS FullName, COUNT(*) AS Cnt
                    FROM Documents d
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.CreatedDate >= @YearStart
                    GROUP BY d.CreatedBy, u.FirstName, u.LastName
                    ORDER BY Cnt DESC";
                using (var cmd = new SqlCommand(topUsersQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@YearStart", thisYear);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader.IsDBNull(1) ? "N/A" : reader.GetString(1).Trim();
                            topUsersList.Add(new { Name = string.IsNullOrEmpty(name) ? "N/A" : name, Count = reader.GetInt32(2) });
                        }
                    }
                }
                ViewBag.TopUsers = topUsersList;
            }

            return View();
        }

        // Helper: execute scalar that returns int (handles nulls)
        private static async Task<int> ExecuteScalarIntAsync(SqlConnection conn, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value)
                    return 0;

                // Try to convert safely
                if (result is int) return (int)result;
                return Convert.ToInt32(result);
            }
        }
    }
}