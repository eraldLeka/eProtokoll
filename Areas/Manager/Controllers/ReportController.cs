using eProtokoll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // === TOTALET ===
            ViewBag.TotalDocuments = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents");

            ViewBag.TotalIncoming = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Type",
                new SqlParameter("@Type", (int)DocumentType.Incoming));

            ViewBag.TotalOutgoing = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Type",
                new SqlParameter("@Type", (int)DocumentType.Outgoing));

            ViewBag.TotalInternal = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = @Type",
                new SqlParameter("@Type", (int)DocumentType.Internal));

            // === SIPAS PERIUDHËS ===
            ViewBag.TodayCount = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today",
                new SqlParameter("@Today", today));

            ViewBag.MonthCount = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @MonthStart",
                new SqlParameter("@MonthStart", thisMonth));

            ViewBag.YearCount = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @YearStart",
                new SqlParameter("@YearStart", thisYear));

            // varesi prioritetit
            ViewBag.High = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE Priority = @Priority",
                new SqlParameter("@Priority", (int)Priority.High));

            ViewBag.Normal = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE Priority = @Priority",
                new SqlParameter("@Priority", (int)Priority.Normal));

            ViewBag.Low = await ExecuteScalarIntAsync(connection,
                "SELECT COUNT(*) FROM Documents WHERE Priority = @Priority",
                new SqlParameter("@Priority", (int)Priority.Low));

            // === TË DHËNAT MUJORE (12 muajt e fundit) ===
            var monthlyData = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(today.AddMonths(-i).Year, today.AddMonths(-i).Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var count = await ExecuteScalarIntAsync(connection,
                    "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @MonthStart AND CreatedDate < @MonthEnd",
                    new SqlParameter("@MonthStart", monthStart),
                    new SqlParameter("@MonthEnd", monthEnd));

                monthlyData.Add(new { Month = monthStart.ToString("MMM yyyy"), Count = count });
            }
            ViewBag.MonthlyData = monthlyData;

            // === TOP 10 INSTITUCIONET ===
            var topInstitutions = new List<object>();
            var topInstQuery = @"
                SELECT TOP 10 
                    ISNULL(i.Name, 'N/A') AS Name, 
                    COUNT(*) AS Cnt
                FROM Documents d
                LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                WHERE d.DocumentType = @Incoming AND d.CreatedDate >= @YearStart
                GROUP BY d.InstitutionId, i.Name
                ORDER BY Cnt DESC";

            using (var cmd = new SqlCommand(topInstQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Incoming", (int)DocumentType.Incoming);
                cmd.Parameters.AddWithValue("@YearStart", thisYear);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    topInstitutions.Add(new
                    {
                        Name = reader.IsDBNull(0) ? "N/A" : reader.GetString(0),
                        Count = reader.GetInt32(1)
                    });
                }
            }
            ViewBag.TopInstitutions = topInstitutions;

            // === TOP 10 PËRDORUESIT ===
            var topUsersList = new List<object>();
            var topUsersQuery = @"
                SELECT TOP 10
                    ISNULL(u.FirstName + ' ' + u.LastName, 'N/A') AS FullName,
                    COUNT(*) AS Cnt
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                WHERE d.CreatedDate >= @YearStart
                GROUP BY d.CreatedBy, u.FirstName, u.LastName
                ORDER BY Cnt DESC";

            using (var cmd = new SqlCommand(topUsersQuery, connection))
            {
                cmd.Parameters.AddWithValue("@YearStart", thisYear);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.IsDBNull(0) ? "N/A" : reader.GetString(0).Trim();
                    topUsersList.Add(new
                    {
                        Name = string.IsNullOrEmpty(name) ? "N/A" : name,
                        Count = reader.GetInt32(1)
                    });
                }
            }
            ViewBag.TopUsers = topUsersList;

            return View();
        }

        private static async Task<int> ExecuteScalarIntAsync(SqlConnection conn, string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, conn);

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value) return 0;
            if (result is int i) return i;
            return Convert.ToInt32(result);
        }
    }
}