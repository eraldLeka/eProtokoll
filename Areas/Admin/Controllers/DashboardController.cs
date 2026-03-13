using eProtokoll.Models;
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
            GetDocumentTypeDistribution();
            GetMonthlyData();

            return View();
        }

        private void GetStatistics()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            ViewBag.TotalUsers = (int)new SqlCommand(
                "SELECT COUNT(*) FROM Users", connection).ExecuteScalar();

            ViewBag.TotalDocuments = (int)new SqlCommand(
                "SELECT COUNT(*) FROM Documents", connection).ExecuteScalar();

            ViewBag.DocumentsToday = (int)new SqlCommand(
                "SELECT COUNT(*) FROM Documents WHERE CAST(ProtocolDate AS DATE) = CAST(GETDATE() AS DATE)",
                connection).ExecuteScalar();

            ViewBag.TotalInstitutions = (int)new SqlCommand(
                "SELECT COUNT(*) FROM Institutions WHERE IsActive = 1", connection).ExecuteScalar();

            ViewBag.UsersThisMonth = (int)new SqlCommand(
                @"SELECT COUNT(*) FROM Users 
                  WHERE MONTH(CreatedDate) = MONTH(GETDATE()) 
                  AND YEAR(CreatedDate) = YEAR(GETDATE())", connection).ExecuteScalar();

            ViewBag.InstitutionsThisMonth = (int)new SqlCommand(
                @"SELECT COUNT(*) FROM Institutions 
                  WHERE MONTH(CreatedDate) = MONTH(GETDATE()) 
                  AND YEAR(CreatedDate) = YEAR(GETDATE())", connection).ExecuteScalar();
        }

        private void GetRecentActivity()
        {
            var activities = new List<dynamic>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var query = @"
                SELECT TOP 5 
                    d.CreatedDate,
                    u.FirstName + ' ' + u.LastName AS UserName,
                    d.ProtocolNumber,
                    d.Subject,
                    d.DocumentType
                FROM Documents d
                INNER JOIN Users u ON d.CreatedBy = u.Id
                ORDER BY d.CreatedDate DESC";

            using var reader = new SqlCommand(query, connection).ExecuteReader();
            while (reader.Read())
            {
                activities.Add(new
                {
                    Time = reader.GetDateTime(0).ToString("HH:mm"),
                    UserName = reader.GetString(1),
                    ProtocolNumber = reader.GetString(2),
                    Subject = reader.GetString(3),
                    DocumentType = (DocumentType)reader.GetInt32(4)
                });
            }

            ViewBag.RecentActivity = activities;
        }

        private void GetDocumentTypeDistribution()
        {
            var incoming = 0;
            var outgoing = 0;
            var internal_doc = 0;

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var reader = new SqlCommand(
                "SELECT DocumentType, COUNT(*) FROM Documents GROUP BY DocumentType",
                connection).ExecuteReader();

            while (reader.Read())
            {
                var docType = reader.GetInt32(0);
                var count = reader.GetInt32(1);

                if (docType == 1) incoming = count;
                else if (docType == 2) outgoing = count;
                else if (docType == 3) internal_doc = count;
            }

            var total = incoming + outgoing + internal_doc;

            ViewBag.Incoming = incoming;
            ViewBag.Outgoing = outgoing;
            ViewBag.Internal = internal_doc;
            ViewBag.Total = total;

            // Përqindjet gati për progress bars në View
            ViewBag.IncomingPct = total > 0 ? incoming * 100 / total : 0;
            ViewBag.OutgoingPct = total > 0 ? outgoing * 100 / total : 0;
            ViewBag.InternalPct = total > 0 ? internal_doc * 100 / total : 0;
        }

        private void GetMonthlyData()
        {
            var months = new List<string>();
            var counts = new List<int>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            for (int i = 5; i >= 0; i--)
            {
                var cmd = new SqlCommand(@"
                    SELECT 
                        FORMAT(DATEADD(MONTH, @offset, GETDATE()), 'MMM yyyy') AS MonthName,
                        COUNT(*) AS Total
                    FROM Documents 
                    WHERE MONTH(ProtocolDate) = MONTH(DATEADD(MONTH, @offset, GETDATE()))
                    AND YEAR(ProtocolDate) = YEAR(DATEADD(MONTH, @offset, GETDATE()))",
                    connection);

                cmd.Parameters.AddWithValue("@offset", -i);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    months.Add(reader.GetString(0));
                    counts.Add(reader.GetInt32(1));
                }
            }

            ViewBag.MonthNames = months;
            ViewBag.MonthlyCounts = counts;
        }
    }
}