using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using eProtokoll.Models;
using eProtokoll.Services.Mappers;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class TaskController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public TaskController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
        }

        // GET: Employee/Task/Index
        public async Task<IActionResult> Index(string filter = "all")
        {
            var userId = "test-user-id"; // TODO: Replace with actual user ID
            var tasks = new List<DocumentTracking>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = BuildFilterQuery(filter);

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tracking = TrackingMapper.MapToDocumentTracking(reader);

                            // Populate Document
                            tracking.Document = new Document
                            {
                                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"))
                            };

                            // Populate AssignedBy User
                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByFirstName")))
                            {
                                tracking.AssignedByUser = new ApplicationUser
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }

                            tasks.Add(tracking);
                        }
                    }
                }
            }

            ViewBag.CurrentFilter = filter;
            return View(tasks);
        }

        // GET: Employee/Task/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = "test-user-id"; // TODO: Replace with actual user ID
            DocumentTracking? tracking = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT dt.*, 
                        d.ProtocolNumber, d.Subject, d.Content, d.Priority, d.DocumentType, d.ProtocolDate,
                        d.ReferenceNumber, d.ReferenceDate, d.HasDeadline, d.DeadlineDate,
                        assignedBy.FirstName as AssignedByFirstName, assignedBy.LastName as AssignedByLastName,
                        assignedTo.FirstName as AssignedToFirstName, assignedTo.LastName as AssignedToLastName
                    FROM DocumentTrackings dt
                    INNER JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN AspNetUsers assignedBy ON dt.AssignedByUserId = assignedBy.Id
                    LEFT JOIN AspNetUsers assignedTo ON dt.AssignedToUserId = assignedTo.Id
                    WHERE dt.TrackingId = @TrackingId 
                    AND dt.AssignedToUserId = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            tracking = TrackingMapper.MapToDocumentTracking(reader);

                            tracking.Document = new Document
                            {
                                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                Content = reader.IsDBNull(reader.GetOrdinal("Content")) ? null : reader.GetString(reader.GetOrdinal("Content")),
                                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                                ProtocolDate = reader.GetDateTime(reader.GetOrdinal("ProtocolDate")),
                                ReferenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                                ReferenceDate = reader.IsDBNull(reader.GetOrdinal("ReferenceDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReferenceDate")),
                                HasDeadline = reader.GetBoolean(reader.GetOrdinal("HasDeadline")),
                                DeadlineDate = reader.IsDBNull(reader.GetOrdinal("DeadlineDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DeadlineDate"))
                            };

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByFirstName")))
                            {
                                tracking.AssignedByUser = new ApplicationUser
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToFirstName")))
                            {
                                tracking.AssignedToUser = new ApplicationUser
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }
                        }
                    }
                }
            }

            if (tracking == null)
            {
                TempData["ErrorMessage"] = "Task nuk u gjet.";
                return RedirectToAction(nameof(Index));
            }

            return View(tracking);
        }

        // POST: Employee/Task/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = "test-user-id"; // TODO: Replace

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    UPDATE DocumentTrackings 
                    SET Status = @Status, 
                        IsAccepted = 1, 
                        AcceptedDate = @Now,
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE TrackingId = @TrackingId 
                    AND AssignedToUserId = @UserId 
                    AND Status = @AssignedStatus";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Status", (int)TrackingStatus.Accepted);
                    command.Parameters.AddWithValue("@AssignedStatus", (int)TrackingStatus.Assigned);
                    command.Parameters.AddWithValue("@Now", DateTime.Now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "✅ Task u pranua me sukses!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "❌ Gabim gjatë pranimit të task.";
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Employee/Task/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Arsyeja e refuzimit është e detyrueshme.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = "test-user-id"; // TODO: Replace

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    UPDATE DocumentTrackings 
                    SET Status = @Status, 
                        IsRejected = 1, 
                        RejectedDate = @Now,
                        RejectionReason = @Reason,
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE TrackingId = @TrackingId 
                    AND AssignedToUserId = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Status", (int)TrackingStatus.Rejected);
                    command.Parameters.AddWithValue("@Reason", reason);
                    command.Parameters.AddWithValue("@Now", DateTime.Now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Task u refuzua.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Gabim gjatë refuzimit.";
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Employee/Task/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = "test-user-id"; // TODO: Replace

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    UPDATE DocumentTrackings 
                    SET IsRead = 1, 
                        ReadDate = @Now,
                        Status = @Status,
                        IsCompleted = 1,
                        CompletedDate = @Now,
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE TrackingId = @TrackingId 
                    AND AssignedToUserId = @UserId 
                    AND ActionType = @ForInformation";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Status", (int)TrackingStatus.Completed);
                    command.Parameters.AddWithValue("@ForInformation", (int)ActionType.ForInformation);
                    command.Parameters.AddWithValue("@Now", DateTime.Now);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "✅ Dokumenti u shënua si i lexuar.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Gabim gjatë shënimit.";
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Employee/Task/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var userId = "test-user-id"; // TODO: Replace
            string? filePath = null;
            string? fileName = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Note: FilePath dhe FileName duhet të ekzistojnë në Document table
                // Nëse nuk ekzistojnë, hiq këtë method ose shto këto columns
                var query = @"
                    SELECT d.FilePath, d.FileName 
                    FROM DocumentTrackings dt
                    INNER JOIN Documents d ON dt.DocumentId = d.DocumentId
                    WHERE dt.TrackingId = @TrackingId 
                    AND dt.AssignedToUserId = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            filePath = reader.IsDBNull(0) ? null : reader.GetString(0);
                            fileName = reader.IsDBNull(1) ? null : reader.GetString(1);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "File nuk u gjet.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", fileName ?? "document.pdf");
        }

        // Helper: Build filter query
        private string BuildFilterQuery(string filter)
        {
            var baseQuery = @"
                SELECT dt.*, 
                    d.DocumentId, d.ProtocolNumber, d.Subject, d.Priority, d.DocumentType,
                    u.FirstName as AssignedByFirstName, u.LastName as AssignedByLastName
                FROM DocumentTrackings dt
                INNER JOIN Documents d ON dt.DocumentId = d.DocumentId
                LEFT JOIN AspNetUsers u ON dt.AssignedByUserId = u.Id
                WHERE dt.AssignedToUserId = @UserId 
                AND dt.IsActive = 1";

            return filter.ToLower() switch
            {
                "pending" => baseQuery + " AND dt.Status = 1 ORDER BY dt.CreatedDate DESC",
                "inprogress" => baseQuery + " AND dt.Status IN (2, 3) ORDER BY dt.DueDate ASC",
                "overdue" => baseQuery + " AND dt.HasDeadline = 1 AND dt.DueDate < @Today AND dt.Status NOT IN (5, 7, 8) ORDER BY dt.DueDate ASC",
                "completed" => baseQuery + " AND dt.Status = 5 ORDER BY dt.CompletedDate DESC",
                _ => baseQuery + " ORDER BY dt.CreatedDate DESC"
            };
        }
    }
}