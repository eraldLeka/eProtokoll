using Microsoft.AspNetCore.Mvc;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using eProtokoll.Services.Mappers;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportController : Controller
    {
        private readonly string _connectionString;

        public ReportController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Admin/Report
        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportDashboardViewModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Statistika të përgjithshme (TPH - Documents table)
                viewModel.TotalDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents");
                viewModel.TotalIncomingDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1");
                viewModel.TotalOutgoingDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2");
                viewModel.TotalInternalDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3");

                viewModel.TotalInstitutions = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Institutions");
                viewModel.ActiveInstitutions = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Institutions WHERE IsActive = 1");

                viewModel.TotalClassifications = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Classifications");

                // Dokumente sipas statusit
                viewModel.DraftDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Status = {(int)DocumentStatus.Draft}");
                viewModel.RegisteredDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Status = {(int)DocumentStatus.Registered}");
                viewModel.InProgressDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Status = {(int)DocumentStatus.InProgress}");
                viewModel.CompletedDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Status = {(int)DocumentStatus.Completed}");

                // Dokumente sipas prioritetit
                viewModel.LowPriorityDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Priority = {(int)Priority.Low}");
                viewModel.NormalPriorityDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Priority = {(int)Priority.Normal}");
                viewModel.HighPriorityDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Priority = {(int)Priority.High}");
                viewModel.UrgentPriorityDocuments = await ExecuteCountQuery(connection, $"SELECT COUNT(*) FROM Documents WHERE Priority = {(int)Priority.Urgent}");

                // Dokumente të muajit aktual
                var query = @"SELECT COUNT(*) FROM Documents 
                    WHERE MONTH(CreatedDate) = @Month AND YEAR(CreatedDate) = @Year";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Month", DateTime.Now.Month);
                    command.Parameters.AddWithValue("@Year", DateTime.Now.Year);
                    var result = await command.ExecuteScalarAsync();
                    viewModel.CurrentMonthDocuments = result != null ? (int)result : 0;
                }

                // Dokumente të javës aktuale
                query = "SELECT COUNT(*) FROM Documents WHERE CreatedDate >= @WeekAgo";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@WeekAgo", DateTime.Now.AddDays(-7));
                    var result = await command.ExecuteScalarAsync();
                    viewModel.CurrentWeekDocuments = result != null ? (int)result : 0;
                }

                // Dokumente të sotme
                query = "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    var result = await command.ExecuteScalarAsync();
                    viewModel.TodayDocuments = result != null ? (int)result : 0;
                }
            }

            return View(viewModel);
        }

        // GET: Admin/Report/DocumentsByType
        public async Task<IActionResult> DocumentsByType(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var documents = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT DocumentType, COUNT(*) as Count 
                    FROM Documents 
                    WHERE CreatedDate >= @StartDate AND CreatedDate <= @EndDate
                    GROUP BY DocumentType";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value);
                    command.Parameters.AddWithValue("@EndDate", endDate.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new
                            {
                                Type = (DocumentType)reader.GetInt32(0),
                                Count = reader.GetInt32(1)
                            });
                        }
                    }
                }
            }

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(documents);
        }

        // GET: Admin/Report/DocumentsByStatus
        public async Task<IActionResult> DocumentsByStatus()
        {
            var documents = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT Status, COUNT(*) as Count 
                    FROM Documents 
                    GROUP BY Status";

                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new
                            {
                                Status = (DocumentStatus)reader.GetInt32(0),
                                Count = reader.GetInt32(1)
                            });
                        }
                    }
                }
            }

            return View(documents);
        }

        // GET: Admin/Report/DocumentsByInstitution
        public async Task<IActionResult> DocumentsByInstitution()
        {
            var incoming = new List<dynamic>();
            var outgoing = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Incoming documents by institution (TPH - Documents with DocumentType = 1)
                var queryIncoming = @"SELECT TOP 10 i.Name, COUNT(*) as Count 
                    FROM Documents d
                    INNER JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    WHERE d.DocumentType = 1
                    GROUP BY i.Name
                    ORDER BY COUNT(*) DESC";

                using (var command = new SqlCommand(queryIncoming, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            incoming.Add(new
                            {
                                Institution = reader.GetString(0),
                                Count = reader.GetInt32(1)
                            });
                        }
                    }
                }

                // Outgoing documents by institution (TPH - Documents with DocumentType = 2)
                var queryOutgoing = @"SELECT TOP 10 i.Name, COUNT(*) as Count 
                    FROM Documents d
                    INNER JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    WHERE d.DocumentType = 2
                    GROUP BY i.Name
                    ORDER BY COUNT(*) DESC";

                using (var command = new SqlCommand(queryOutgoing, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            outgoing.Add(new
                            {
                                Institution = reader.GetString(0),
                                Count = reader.GetInt32(1)
                            });
                        }
                    }
                }
            }

            ViewBag.IncomingDocuments = incoming;
            ViewBag.OutgoingDocuments = outgoing;

            return View();
        }

        // GET: Admin/Report/MonthlyReport
        public async Task<IActionResult> MonthlyReport(int? year, int? month)
        {
            year ??= DateTime.Now.Year;
            month ??= DateTime.Now.Month;

            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT * FROM Documents 
                    WHERE CreatedDate >= @StartDate AND CreatedDate <= @EndDate
                    ORDER BY CreatedDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(DocumentMapper.MapToDocument(reader));
                        }
                    }
                }
            }

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.MonthName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");

            return View(documents);
        }

        // GET: Admin/Report/ExportData
        public IActionResult ExportData()
        {
            return View();
        }

        // GET: Admin/Report/AuditLog
        public async Task<IActionResult> AuditLog(DateTime? startDate, DateTime? endDate, string userId = null, string actionType = null)
        {
            startDate ??= DateTime.Now.AddDays(-30);
            endDate ??= DateTime.Now;

            var auditLogs = new List<DocumentTracking>();
            var users = new List<dynamic>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // ✅ FIKSUAR: AssignedToUserId dhe AssignedByUserId (jo AssignedTo/AssignedBy)
                var query = @"SELECT dt.*, 
                    d.ProtocolNumber, d.Subject,
                    u1.UserName as AssignedToUserName, u1.FirstName as AssignedToFirstName, u1.LastName as AssignedToLastName,
                    u2.UserName as AssignedByUserName, u2.FirstName as AssignedByFirstName, u2.LastName as AssignedByLastName
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN AspNetUsers u1 ON dt.AssignedToUserId = u1.Id
                    LEFT JOIN AspNetUsers u2 ON dt.AssignedByUserId = u2.Id
                    WHERE dt.CreatedDate >= @StartDate AND dt.CreatedDate <= @EndDate";

                // Add user filter
                if (!string.IsNullOrEmpty(userId))
                {
                    query += " AND (dt.AssignedToUserId = @UserId OR dt.AssignedByUserId = @UserId)";
                }

                // Add action type filter
                if (!string.IsNullOrEmpty(actionType) && Enum.TryParse<ActionType>(actionType, out var action))
                {
                    query += " AND dt.ActionType = @ActionType";
                }

                query += " ORDER BY dt.CreatedDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value);
                    command.Parameters.AddWithValue("@EndDate", endDate.Value);

                    if (!string.IsNullOrEmpty(userId))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                    }

                    if (!string.IsNullOrEmpty(actionType) && Enum.TryParse<ActionType>(actionType, out var parsedAction))
                    {
                        command.Parameters.AddWithValue("@ActionType", (int)parsedAction);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tracking = TrackingMapper.MapToDocumentTracking(reader);

                            // Manually populate navigation properties
                            if (!reader.IsDBNull(reader.GetOrdinal("ProtocolNumber")))
                            {
                                tracking.Document = new Document
                                {
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.GetString(reader.GetOrdinal("Subject"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToUserName")))
                            {
                                tracking.AssignedToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedToUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByUserName")))
                            {
                                tracking.AssignedByUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedByUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }

                            auditLogs.Add(tracking);
                        }
                    }
                }

                // Get users for dropdown
                var queryUsers = "SELECT Id, UserName FROM AspNetUsers ORDER BY UserName";
                using (var command = new SqlCommand(queryUsers, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new
                            {
                                Id = reader.GetString(0),
                                UserName = reader.GetString(1)
                            });
                        }
                    }
                }
            }

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedActionType = actionType;
            ViewBag.Users = users;

            return View(auditLogs);
        }

        // Helper method për COUNT queries
        private async Task<int> ExecuteCountQuery(SqlConnection connection, string query)
        {
            using (var command = new SqlCommand(query, connection))
            {
                var result = await command.ExecuteScalarAsync();
                return result != null ? (int)result : 0;
            }
        }
    }

    // ViewModel për Dashboard
    public class ReportDashboardViewModel
    {
        public int TotalDocuments { get; set; }
        public int TotalIncomingDocuments { get; set; }
        public int TotalOutgoingDocuments { get; set; }
        public int TotalInternalDocuments { get; set; }

        public int TotalInstitutions { get; set; }
        public int ActiveInstitutions { get; set; }

        public int TotalClassifications { get; set; }

        public int DraftDocuments { get; set; }
        public int RegisteredDocuments { get; set; }
        public int InProgressDocuments { get; set; }
        public int CompletedDocuments { get; set; }

        public int LowPriorityDocuments { get; set; }
        public int NormalPriorityDocuments { get; set; }
        public int HighPriorityDocuments { get; set; }
        public int UrgentPriorityDocuments { get; set; }

        public int CurrentMonthDocuments { get; set; }
        public int CurrentWeekDocuments { get; set; }
        public int TodayDocuments { get; set; }
    }
}