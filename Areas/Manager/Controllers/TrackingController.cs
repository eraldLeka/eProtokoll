using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Security.Claims;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class TrackingController : Controller
    {
        private readonly string _connectionString;

        public TrackingController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Manager/Tracking
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            var pageSize = 20;
            var trackings = new List<DocumentTracking>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var whereClause = new StringBuilder(" WHERE 1=1");
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    whereClause.Append(@" AND (dt.Notes LIKE @SearchTerm 
                        OR d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm)");
                    parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                var countQuery = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId");
                countQuery.Append(whereClause);

                int totalItems;
                using (var countCommand = new SqlCommand(countQuery.ToString(), connection))
                {
                    countCommand.Parameters.AddRange(parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToArray());
                    var result = await countCommand.ExecuteScalarAsync();
                    totalItems = result != null ? Convert.ToInt32(result) : 0;
                }

                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var mainQuery = new StringBuilder(@"
                    SELECT dt.*, 
                        d.ProtocolNumber, d.Subject, d.DocumentType,
                        uat.Id as AssignedToUserId, uat.UserName as AssignedToUserName, uat.FirstName as AssignedToFirstName, uat.LastName as AssignedToLastName,
                        uab.Id as AssignedByUserId, uab.UserName as AssignedByUserName, uab.FirstName as AssignedByFirstName, uab.LastName as AssignedByLastName
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN AspNetUsers uat ON dt.AssignedToUserId = uat.Id
                    LEFT JOIN AspNetUsers uab ON dt.AssignedByUserId = uab.Id");
                mainQuery.Append(whereClause);
                mainQuery.Append(@" ORDER BY dt.AssignedDate DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

                parameters.Add(new SqlParameter("@Offset", (page - 1) * pageSize));
                parameters.Add(new SqlParameter("@PageSize", pageSize));

                using (var command = new SqlCommand(mainQuery.ToString(), connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tracking = TrackingMapper.MapToDocumentTracking(reader);

                            if (!reader.IsDBNull(reader.GetOrdinal("ProtocolNumber")))
                            {
                                tracking.Document = new Document
                                {
                                    DocumentId = tracking.DocumentId,
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.IsDBNull(reader.GetOrdinal("Subject")) ? null : reader.GetString(reader.GetOrdinal("Subject")),
                                    DocumentType = reader.IsDBNull(reader.GetOrdinal("DocumentType")) ? DocumentType.Incoming : (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToUserName")))
                            {
                                tracking.AssignedToUser = new Users
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("AssignedToUserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedToUserName")),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("AssignedToFirstName")) ? null : reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("AssignedToLastName")) ? null : reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByUserName")))
                            {
                                tracking.AssignedByUser = new Users
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("AssignedByUserId")),
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedByUserName")),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("AssignedByFirstName")) ? null : reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("AssignedByLastName")) ? null : reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }

                            trackings.Add(tracking);
                        }
                    }
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                ViewBag.TotalTrackings = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM DocumentTrackings");

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM DocumentTrackings WHERE CAST(AssignedDate AS DATE) = @Today", connection))
                {
                    cmd.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    ViewBag.TodayAssigned = (int)await cmd.ExecuteScalarAsync();
                }

                ViewBag.Active = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM DocumentTrackings WHERE IsActive = 1");
                ViewBag.Completed = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM DocumentTrackings WHERE CompletedDate IS NOT NULL");

                using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM DocumentTrackings 
                    WHERE DueDate < @Now AND CompletedDate IS NULL", connection))
                {
                    cmd.Parameters.AddWithValue("@Now", DateTime.Now);
                    ViewBag.Overdue = (int)await cmd.ExecuteScalarAsync();
                }
            }

            return View(trackings);
        }

        // GET: Manager/Tracking/Assign
        public async Task<IActionResult> Assign(int? documentId)
        {

            var tracking = new DocumentTracking
            {
                AssignedDate = DateTime.Now,
                Priority = Priority.Normal,
                IsActive = true
            };

            // Nëse vjen me documentId, vendos atë dokument si të zgjedhur
            if (documentId.HasValue)
            {
                tracking.DocumentId = documentId.Value;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"SELECT d.*, c.Name as ClassificationName 
                        FROM Documents d
                        LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                        WHERE d.DocumentId = @DocumentId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentId", documentId.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var document = DocumentMapper.MapToDocument(reader);
                                if (!reader.IsDBNull(reader.GetOrdinal("ClassificationName")))
                                {
                                    document.Classification = new Classification
                                    {
                                        ClassificationId = document.ClassificationId,
                                        Name = reader.GetString(reader.GetOrdinal("ClassificationName"))
                                    };
                                }
                                ViewBag.Document = document;
                            }
                        }
                    }
                }
            }

            await LoadDropdowns(documentId);
            return View(tracking);
        }

        // POST: Manager/Tracking/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(DocumentTracking model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.AssignedByUserId = userId;
            ModelState.Remove(nameof(model.AssignedByUserId));

            if (model.DocumentId <= 0)
            {
                TempData["ErrorMessage"] = "Ju lutemi, zgjidhni një dokument nga lista.";
                await LoadDropdowns(model.DocumentId);
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errors) ? "Të dhënat e formularit janë të paqarta." : "ModelState invalid: " + errors;
                await LoadDropdowns(model.DocumentId);
                return View(model);
            }

            // Model is valid - perform insert
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Documents WHERE DocumentId = @DocumentId", connection))
                {
                    checkCmd.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                    var cnt = await checkCmd.ExecuteScalarAsync();
                    var exists = cnt != null ? Convert.ToInt32(cnt) : 0;
                    if (exists == 0)
                    {
                        TempData["ErrorMessage"] = $"Dokumenti me ID {model.DocumentId} nuk ekziston në sistem. Zgjidhni një dokument të vlefshëm.";
                        await LoadDropdowns(model.DocumentId);
                        return View(model);
                    }
                }

                try
                {
                    var query = @"INSERT INTO DocumentTrackings (
                            DocumentId, AssignedToUserId, AssignedByUserId, AssignedDate, 
                            Priority, DueDate, Notes, IsActive, CreatedDate
                        ) VALUES (
                            @DocumentId, @AssignedToUserId, @AssignedByUserId, @AssignedDate, 
                            @Priority, @DueDate, @Notes, @IsActive, @CreatedDate
                        )";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                        command.Parameters.AddWithValue("@AssignedToUserId", model.AssignedToUserId);
                        command.Parameters.AddWithValue("@AssignedByUserId", (object)userId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AssignedDate", model.AssignedDate);
                        command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                        command.Parameters.AddWithValue("@DueDate", (object)model.DueDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                        command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        await command.ExecuteNonQueryAsync();
                    }

                    TempData["SuccessMessage"] = "Dokumenti u caktua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Gabim gjatë caktimit: {ex.Message}";
                    await LoadDropdowns(model.DocumentId);
                    return View(model);
                }
            }
        }

        // GET: Manager/Tracking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            DocumentTracking tracking = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"SELECT dt.*, 
                    d.ProtocolNumber, d.Subject, d.DocumentType, d.ClassificationId,
                    c.Name as ClassificationName,
                    uat.UserName as AssignedToUserName, uat.FirstName as AssignedToFirstName, uat.LastName as AssignedToLastName,
                    uab.UserName as AssignedByUserName, uab.FirstName as AssignedByFirstName, uab.LastName as AssignedByLastName
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers uat ON dt.AssignedToUserId = uat.Id
                    LEFT JOIN AspNetUsers uab ON dt.AssignedByUserId = uab.Id
                    WHERE dt.TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            tracking = TrackingMapper.MapToDocumentTracking(reader);

                            if (!reader.IsDBNull(reader.GetOrdinal("ProtocolNumber")))
                            {
                                tracking.Document = new Document
                                {
                                    DocumentId = tracking.DocumentId,
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.IsDBNull(reader.GetOrdinal("Subject")) ? null : reader.GetString(reader.GetOrdinal("Subject")),
                                    DocumentType = reader.IsDBNull(reader.GetOrdinal("DocumentType")) ? DocumentType.Incoming : (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                                    ClassificationId = reader.IsDBNull(reader.GetOrdinal("ClassificationId")) ? 0 : reader.GetInt32(reader.GetOrdinal("ClassificationId"))
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("ClassificationName")))
                                {
                                    tracking.Document.Classification = new Classification
                                    {
                                        ClassificationId = tracking.Document.ClassificationId,
                                        Name = reader.GetString(reader.GetOrdinal("ClassificationName"))
                                    };
                                }
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToUserName")))
                            {
                                tracking.AssignedToUser = new Users
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedToUserName")),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("AssignedToFirstName")) ? null : reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("AssignedToLastName")) ? null : reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByUserName")))
                            {
                                tracking.AssignedByUser = new Users
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedByUserName")),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("AssignedByFirstName")) ? null : reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("AssignedByLastName")) ? null : reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }
                        }
                    }
                }

                if (tracking == null) return NotFound();

                tracking.Document.Attachments = new List<DocumentAttachment>();
                using (var command = new SqlCommand("SELECT * FROM DocumentAttachments WHERE DocumentId = @DocumentId ORDER BY DisplayOrder", connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", tracking.DocumentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tracking.Document.Attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));
                        }
                    }
                }
            }

            return View(tracking);
        }

        // POST: Manager/Tracking/Complete/5
        [HttpPost]
        public async Task<IActionResult> Complete(int id, string? comment)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE DocumentTrackings SET
                    CompletedDate = @CompletedDate,
                    Notes = CASE WHEN Notes IS NULL OR LEN(RTRIM(Notes)) = 0 THEN @Comment ELSE Notes + CHAR(13) + CHAR(10) + @Comment END,
                    IsActive = 0
                    WHERE TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@CompletedDate", DateTime.Now);
                    var user = User.Identity?.Name ?? "System";
                    var note = string.IsNullOrWhiteSpace(comment) ? $"Completed by {user} at {DateTime.Now}" : $"Completed by {user} at {DateTime.Now}: {comment}";
                    command.Parameters.AddWithValue("@Comment", note);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Gjurmimi u përfundua me sukses!" });
                    else
                        return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });
                }
            }
        }

        // POST: Manager/Tracking/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE DocumentTrackings SET
                    IsActive = 0,
                    Notes = CASE WHEN Notes IS NULL OR LEN(RTRIM(Notes)) = 0 THEN @Notes ELSE Notes + CHAR(13) + CHAR(10) + @Notes END
                    WHERE TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    var user = User.Identity?.Name ?? "System";
                    command.Parameters.AddWithValue("@Notes", $"Cancelled by {user} at {DateTime.Now}: {reason}");

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Gjurmimi u anullua me sukses!" });
                    else
                        return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });
                }
            }
        }

        // POST: Manager/Tracking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                try
                {
                    using (var command = new SqlCommand("DELETE FROM DocumentTrackings WHERE TrackingId = @TrackingId", connection))
                    {
                        command.Parameters.AddWithValue("@TrackingId", id);
                        var rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                            TempData["SuccessMessage"] = "Gjurmimi u fshi me sukses!";
                        else
                            TempData["ErrorMessage"] = "Gjurmimi nuk u gjet!";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // ========== HELPER METHODS ==========

        private async Task LoadDropdowns(int? selectedDocumentId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var documents = new List<dynamic>();
                var documentQuery = @"
                    SELECT d.DocumentId, d.ProtocolNumber, d.Subject 
                    FROM Documents d
                    ORDER BY d.CreatedDate DESC";

                using (var command = new SqlCommand(documentQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var protocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber"));
                            var subject = reader.IsDBNull(reader.GetOrdinal("Subject"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("Subject"));

                            documents.Add(new
                            {
                                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                                DisplayText = $"{protocolNumber} - {subject}"
                            });
                        }
                    }
                }
                ViewBag.Documents = new SelectList(documents, "DocumentId", "DisplayText", selectedDocumentId);

                // Merr përdoruesit aktivë
                var users = new List<Users>();
                using (var command = new SqlCommand(@"
                    SELECT Id, UserName, FirstName, LastName 
                    FROM AspNetUsers 
                    WHERE IsActive = 1 
                    ORDER BY FirstName, LastName", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var user = new Users
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.IsDBNull(reader.GetOrdinal("LastName"))
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("LastName"))
                            };
                            users.Add(user);
                        }
                    }
                }
                ViewBag.Users = new SelectList(users, "Id", "FullName");
            }
        }

        private async Task<int> ExecuteCountQuery(SqlConnection connection, string query)
        {
            using (var command = new SqlCommand(query, connection))
            {
                var result = await command.ExecuteScalarAsync();
                return result != null ? (int)result : 0;
            }
        }
    }
}