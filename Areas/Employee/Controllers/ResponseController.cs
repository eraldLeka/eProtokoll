using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using eProtokoll.Models;
using eProtokoll.Services.Mappers;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class ResponseController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public ResponseController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
        }

        // GET: Employee/Response/Create/5
        public async Task<IActionResult> Create(int trackingId)
        {
            var userId = "test-user-id"; // TODO: Replace
            DocumentTracking? tracking = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT dt.*, 
                        d.ProtocolNumber, d.Subject, d.Priority, d.DocumentType
                    FROM DocumentTrackings dt
                    INNER JOIN Documents d ON dt.DocumentId = d.DocumentId
                    WHERE dt.TrackingId = @TrackingId 
                    AND dt.AssignedToUserId = @UserId
                    AND dt.ActionType = @ForResponse";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", trackingId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ForResponse", (int)ActionType.ForResponse);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            tracking = TrackingMapper.MapToDocumentTracking(reader);
                            tracking.Document = new Document
                            {
                                ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority")),
                                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"))
                            };
                        }
                    }
                }
            }

            if (tracking == null)
            {
                TempData["ErrorMessage"] = "Task nuk u gjet ose nuk kërkon përgjigje.";
                return RedirectToAction("Index", "Task");
            }

            if (tracking.Status == TrackingStatus.Assigned)
            {
                TempData["WarningMessage"] = "Duhet ta pranosh task-in para se të ngarkosh përgjigjen.";
                return RedirectToAction("Details", "Task", new { id = trackingId });
            }

            ViewBag.Tracking = tracking;
            return View();
        }

        // POST: Employee/Response/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int trackingId, string responseSubject, string? responseNotes, IFormFile scannedPdf)
        {
            var userId = "test-user-id"; // TODO: Replace

            // Validation
            if (string.IsNullOrWhiteSpace(responseSubject))
            {
                TempData["ErrorMessage"] = "Subjekti i përgjigjes është i detyrueshëm.";
                return RedirectToAction(nameof(Create), new { trackingId });
            }

            if (scannedPdf == null || scannedPdf.Length == 0)
            {
                TempData["ErrorMessage"] = "PDF-ja e skanuar është e detyrueshme.";
                return RedirectToAction(nameof(Create), new { trackingId });
            }

            if (!scannedPdf.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Vetëm PDF files janë të lejuara.";
                return RedirectToAction(nameof(Create), new { trackingId });
            }

            if (scannedPdf.Length > 10 * 1024 * 1024) // 10MB
            {
                TempData["ErrorMessage"] = "PDF-ja nuk mund të jetë më e madhe se 10MB.";
                return RedirectToAction(nameof(Create), new { trackingId });
            }

            try
            {
                // Save file
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "responses", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"));
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"RESP_{trackingId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await scannedPdf.CopyToAsync(stream);
                }

                // Save to database
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        INSERT INTO DocumentResponses 
                        (TrackingId, ResponseSubject, ResponseNotes, ScannedPdfPath, ScannedPdfName, ScannedPdfSize, 
                         Status, CreatedDate, IsActive)
                        VALUES 
                        (@TrackingId, @Subject, @Notes, @Path, @Name, @Size, @Status, @Now, 1);
                        
                        UPDATE DocumentTrackings 
                        SET Status = @InProgress, 
                            IsInProgress = 1, 
                            StartedDate = @Now,
                            ModifiedDate = @Now,
                            ModifiedBy = @UserId
                        WHERE TrackingId = @TrackingId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TrackingId", trackingId);
                        command.Parameters.AddWithValue("@Subject", responseSubject);
                        command.Parameters.AddWithValue("@Notes", responseNotes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Path", filePath);
                        command.Parameters.AddWithValue("@Name", scannedPdf.FileName);
                        command.Parameters.AddWithValue("@Size", scannedPdf.Length);
                        command.Parameters.AddWithValue("@Status", (int)ResponseStatus.Submitted);
                        command.Parameters.AddWithValue("@InProgress", (int)TrackingStatus.InProgress);
                        command.Parameters.AddWithValue("@Now", DateTime.Now);
                        command.Parameters.AddWithValue("@UserId", userId);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = "✅ Përgjigja u ngarkua me sukses dhe u dërgua për aprovim!";
                return RedirectToAction("Index", "Task");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë ngarkimit: {ex.Message}";
                return RedirectToAction(nameof(Create), new { trackingId });
            }
        }

        // GET: Employee/Response/History
        public async Task<IActionResult> History()
        {
            var userId = "test-user-id"; // TODO: Replace
            var responses = new List<DocumentResponse>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT dr.*, 
                        dt.TrackingId,
                        d.ProtocolNumber, d.Subject, d.Priority,
                        approver.FirstName as ApproverFirstName, approver.LastName as ApproverLastName
                    FROM DocumentResponses dr
                    INNER JOIN DocumentTrackings dt ON dr.TrackingId = dt.TrackingId
                    INNER JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN AspNetUsers approver ON dr.ApprovedByUserId = approver.Id
                    WHERE dt.AssignedToUserId = @UserId 
                    AND dr.IsActive = 1
                    ORDER BY dr.CreatedDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var response = new DocumentResponse
                            {
                                ResponseId = reader.GetInt32(reader.GetOrdinal("ResponseId")),
                                TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                                ResponseSubject = reader.GetString(reader.GetOrdinal("ResponseSubject")),
                                ResponseNotes = reader.IsDBNull(reader.GetOrdinal("ResponseNotes")) ? null : reader.GetString(reader.GetOrdinal("ResponseNotes")),
                                ScannedPdfPath = reader.GetString(reader.GetOrdinal("ScannedPdfPath")),
                                ScannedPdfName = reader.GetString(reader.GetOrdinal("ScannedPdfName")),
                                ScannedPdfSize = reader.GetInt64(reader.GetOrdinal("ScannedPdfSize")),
                                Status = (ResponseStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                SubmittedDate = reader.IsDBNull(reader.GetOrdinal("SubmittedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedDate")),
                                ApprovedDate = reader.IsDBNull(reader.GetOrdinal("ApprovedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ApprovedDate")),
                                RejectedDate = reader.IsDBNull(reader.GetOrdinal("RejectedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("RejectedDate")),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason")) ? null : reader.GetString(reader.GetOrdinal("RejectionReason"))
                            };

                            // Populate Tracking with Document
                            response.Tracking = new DocumentTracking
                            {
                                TrackingId = reader.GetInt32(reader.GetOrdinal("TrackingId")),
                                Document = new Document
                                {
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                    Priority = (Priority)reader.GetInt32(reader.GetOrdinal("Priority"))
                                }
                            };

                            // Populate Approver
                            if (!reader.IsDBNull(reader.GetOrdinal("ApproverFirstName")))
                            {
                                response.ApprovedByUser = new ApplicationUser
                                {
                                    FirstName = reader.GetString(reader.GetOrdinal("ApproverFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("ApproverLastName"))
                                };
                            }

                            responses.Add(response);
                        }
                    }
                }
            }

            return View(responses);
        }

        // GET: Employee/Response/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var userId = "test-user-id"; // TODO: Replace
            string? filePath = null;
            string? fileName = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT dr.ScannedPdfPath, dr.ScannedPdfName 
                    FROM DocumentResponses dr
                    INNER JOIN DocumentTrackings dt ON dr.TrackingId = dt.TrackingId
                    WHERE dr.ResponseId = @ResponseId 
                    AND dt.AssignedToUserId = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResponseId", id);
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            filePath = reader.GetString(0);
                            fileName = reader.GetString(1);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "File nuk u gjet.";
                return RedirectToAction(nameof(History));
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", fileName ?? "response.pdf");
        }
    }
}