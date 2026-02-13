using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            GetStatistics();
            GetRecentActivity();
            GetChartData();

            return View();
        }

        private void GetStatistics()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Users count
                var usersCmd = new SqlCommand("SELECT COUNT(*) FROM Users", connection);
                ViewBag.TotalUsers = (int)usersCmd.ExecuteScalar();

                // Documents count
                var docsCmd = new SqlCommand("SELECT COUNT(*) FROM Documents", connection);
                ViewBag.TotalDocuments = (int)docsCmd.ExecuteScalar();

                // Documents today
                var docsTodayCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Documents WHERE CAST(ProtocolDate AS DATE) = CAST(GETDATE() AS DATE)",
                    connection);
                ViewBag.DocumentsToday = (int)docsTodayCmd.ExecuteScalar();

                // Institutions count
                var instCmd = new SqlCommand("SELECT COUNT(*) FROM Institutions WHERE IsActive = 1", connection);
                ViewBag.TotalInstitutions = (int)instCmd.ExecuteScalar();

                // Active deadlines
                var deadlinesCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Deadlines WHERE IsCompleted = 0",
                    connection);
                ViewBag.ActiveDeadlines = (int)deadlinesCmd.ExecuteScalar();

                // Users this month
                var usersMonthCmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM Users 
                      WHERE MONTH(CreatedDate) = MONTH(GETDATE()) 
                      AND YEAR(CreatedDate) = YEAR(GETDATE())",
                    connection);
                ViewBag.UsersThisMonth = (int)usersMonthCmd.ExecuteScalar();

                // Institutions this month
                var instMonthCmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM Institutions 
                      WHERE MONTH(CreatedDate) = MONTH(GETDATE()) 
                      AND YEAR(CreatedDate) = YEAR(GETDATE())",
                    connection);
                ViewBag.InstitutionsThisMonth = (int)instMonthCmd.ExecuteScalar();
            }
        }

        private void GetRecentActivity()
        {
            var activities = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var query = @"
                    SELECT TOP 5 
                        d.ProtocolDate,
                        u.FirstName + ' ' + u.LastName as UserName,
                        d.ProtocolNumber,
                        d.Subject,
                        d.DocumentType,
                        d.Status
                    FROM Documents d
                    INNER JOIN Users u ON d.CreatedBy = u.Id
                    ORDER BY d.ProtocolDate DESC";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        activities.Add(new
                        {
                            Time = reader.GetDateTime(0).ToString("HH:mm"),
                            UserName = reader.GetString(1),
                            ProtocolNumber = reader.GetString(2),
                            Subject = reader.GetString(3),
                            DocumentType = reader.GetInt32(4),
                            Status = reader.GetInt32(5)
                        });
                    }
                }
            }

            ViewBag.RecentActivity = activities;
        }

        private void GetChartData()
        {
            var monthlyData = new List<int>();
            var documentTypes = new { Incoming = 0, Outgoing = 0, Internal = 0 };

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Monthly document counts (last 6 months)
                for (int i = 5; i >= 0; i--)
                {
                    var monthQuery = @"
                        SELECT COUNT(*) FROM Documents 
                        WHERE MONTH(ProtocolDate) = MONTH(DATEADD(MONTH, @MonthOffset, GETDATE()))
                        AND YEAR(ProtocolDate) = YEAR(DATEADD(MONTH, @MonthOffset, GETDATE()))";

                    using (var cmd = new SqlCommand(monthQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@MonthOffset", -i);
                        monthlyData.Add((int)cmd.ExecuteScalar());
                    }
                }

                // Document types distribution
                var typeQuery = @"
                    SELECT DocumentType, COUNT(*) as Count
                    FROM Documents
                    GROUP BY DocumentType";

                using (var cmd = new SqlCommand(typeQuery, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    var incoming = 0;
                    var outgoing = 0;
                    var internal_doc = 0;

                    while (reader.Read())
                    {
                        var docType = reader.GetInt32(0);
                        var count = reader.GetInt32(1);

                        if (docType == 1) incoming = count;
                        else if (docType == 2) outgoing = count;
                        else if (docType == 3) internal_doc = count;
                    }

                    documentTypes = new { Incoming = incoming, Outgoing = outgoing, Internal = internal_doc };
                }
            }

            ViewBag.MonthlyData = monthlyData;
            ViewBag.DocumentTypes = documentTypes;
        }
    }
}