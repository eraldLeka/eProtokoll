using eProtokoll.Models;
using eProtokoll.Services.Files;
using eProtokoll.Services.Mappers;
using eProtokoll.Services.ProtocolNumber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using DocumentType = eProtokoll.Models.DocumentType;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class OutgoingDocumentController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private readonly IProtocolNumberService _protocolNumberService;
        private readonly FileService _fileService;


        public OutgoingDocumentController(IConfiguration configuration, IWebHostEnvironment environment, IProtocolNumberService protocolNumberService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
            _protocolNumberService = protocolNumberService;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "outgoing");
            _fileService = new FileService(uploadsFolder);
        }

        // GET: Manager/OutgoingDocument
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string institution = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var documents = new List<OutgoingDocument>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Build dynamic query
                var queryBuilder = new StringBuilder(@"
                    SELECT d.*, 
                        i.Name as InstitutionName, i.ShortName as InstitutionShortName,
                        c.Name as ClassificationName, c.ColorCode,
                        u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentType = @DocumentType");

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@DocumentType", (int)DocumentType.Outgoing)
                };

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.RecipientName LIKE @SearchTerm
                        OR d.Content LIKE @SearchTerm)");
                    parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                // Get total count
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    WHERE d.DocumentType = @DocumentTypeCount");

                var countParams = new List<SqlParameter>
                {
                    new SqlParameter("@DocumentTypeCount", (int)DocumentType.Outgoing)
                };

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.RecipientName LIKE @SearchTerm
                        OR d.Content LIKE @SearchTerm)");
                    countParams.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                int totalItems;
                using (var countCommand = new SqlCommand(countQueryBuilder.ToString(), connection))
                {
                    countCommand.Parameters.AddRange(countParams.ToArray());
                    var result = await countCommand.ExecuteScalarAsync();
                    totalItems = result != null ? Convert.ToInt32(result) : 0;
                }

                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Add sorting and pagination
                queryBuilder.Append(@" ORDER BY d.CreatedDate DESC, d.ProtocolDate DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

                parameters.Add(new SqlParameter("@Offset", (page - 1) * pageSize));
                parameters.Add(new SqlParameter("@PageSize", pageSize));

                // Execute main query
                using (var command = new SqlCommand(queryBuilder.ToString(), connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var document = DocumentMapper.MapToOutgoingDocument(reader);

                            // Populate Institution
                            if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
                            {
                                document.Institution = new Institution
                                {
                                    InstitutionId = document.InstitutionId,
                                    Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                                    ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("InstitutionShortName"))
                                };
                            }

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
                                document.Creator = new Users
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                                };
                            }

                            documents.Add(document);
                        }
                    }
                }

                // ViewBag for search and paging
                ViewBag.SearchTerm = searchTerm;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                // Load institutions for dropdown
                await LoadDropdowns(connection);
            }

            return View(documents);
        }

        // GET: Manager/OutgoingDocument/Create
        public async Task<IActionResult> Create()
        {
            // Gjenero numrin vetëm këtu
            var protocolNumber = await _protocolNumberService.GenerateNextProtocolNumberAsync(eProtokoll.Services.ProtocolNumber.DocumentType.Outgoing);
            var now = DateTime.Now;

            var document = new OutgoingDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = now.Date,
                ProtocolTime = new TimeSpan(now.Hour, now.Minute, now.Second),
                Status = DocumentStatus.Registered,
                Priority = Priority.Normal
            };

            await LoadDropdowns();
            return View(document);
        }

        // POST: Manager/OutgoingDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OutgoingDocument model, IFormFile? attachmentFile)
        {
            var protocolNumber = await _protocolNumberService.GenerateNextProtocolNumberAsync(eProtokoll.Services.ProtocolNumber.DocumentType.Outgoing);
            model.ProtocolNumber = protocolNumber;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var now = DateTime.Now;
            model.ProtocolDate = now.Date;
            model.ProtocolTime = new TimeSpan(now.Hour, now.Minute, now.Second);
            model.Status = DocumentStatus.Registered;
            model.Priority = Priority.Normal;

            // HEQ FUSHAT QË GJENEROHEN AUTOMATIKISHT
            ModelState.Remove(nameof(model.ProtocolNumber));
            ModelState.Remove(nameof(model.ProtocolDate));
            ModelState.Remove(nameof(model.ProtocolTime));
            ModelState.Remove(nameof(model.Status));
            ModelState.Remove(nameof(model.Priority));
            ModelState.Remove(nameof(model.CreatedDate));
            ModelState.Remove(nameof(model.CreatedBy));
            ModelState.Remove(nameof(model.DocumentType));

            // Navigation properties (opsionale por të rekomanduara)
            ModelState.Remove(nameof(model.Institution));
            ModelState.Remove(nameof(model.Classification));
            ModelState.Remove(nameof(model.Creator));
            ModelState.Remove(nameof(model.Attachments));
            ModelState.Remove(nameof(model.Trackings));
            ModelState.Remove(nameof(model.Deadlines));
            ModelState.Remove(nameof(model.OriginalIncomingDocument));

            // Valido skedarin
            if (attachmentFile == null || attachmentFile.Length == 0)
            {
                ModelState.AddModelError("attachmentFile", "Ngarko pdf per dokumentin dalës.");
            }
            else
            {
                var ext = Path.GetExtension(attachmentFile.FileName).ToLower();
                if (ext != ".pdf")
                {
                    ModelState.AddModelError("attachmentFile", "Vetem PDF lejohet");
                }
            }

            if (ModelState.IsValid)
            {
                model.CreatedDate = DateTime.Now;
                model.CreatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                model.DocumentType = DocumentType.Outgoing;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert dokumenti
                            var query = @"INSERT INTO Documents (
                        ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                        ClassificationId, Status, Priority,
                        Notes,HasAttachments, CreatedDate, CreatedBy,
                        InstitutionId, RecipientName, IsResponse, OriginalIncomingDocumentId,
                        ArchiveLocation, Discriminator
                    ) OUTPUT INSERTED.DocumentId VALUES (
                        @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                        @ClassificationId, @Status, @Priority,
                        @Notes,@HasAttachments, @CreatedDate, @CreatedBy,
                        @InstitutionId, @RecipientName, @IsResponse, @OriginalIncomingDocumentId,
                        @ArchiveLocation, 'OutgoingDocument'
                    )";

                            int documentId;
                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                AddOutgoingDocumentParameters(command, model);
                                documentId = (int)await command.ExecuteScalarAsync();
                            }

                            // Handle file upload
                            if (attachmentFile != null && attachmentFile.Length > 0)
                            {
                                using var ms = new MemoryStream();
                                await attachmentFile.CopyToAsync(ms);
                                var fileBytes = ms.ToArray();
                                model.CreatedBy = userId;


                                var savedFile = _fileService.SaveFile(
                                     fileBytes,
                                     attachmentFile.FileName,
                                     documentId,           
                                     attachmentFile.ContentType,
                                     userId
                                );

                                var attachQuery = @"
                            INSERT INTO DocumentAttachments (
                                DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                                ContentType, UploadedDate, UploadedBy, Category, DisplayOrder, IsPrimaryDocument, FileHash
                            ) VALUES (
                                @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                                @ContentType, @UploadedDate, @UploadedBy, @Category, @DisplayOrder, @IsPrimaryDocument, @FileHash
                            )";

                                using (var attachCommand = new SqlCommand(attachQuery, connection, transaction))
                                {
                                    attachCommand.Parameters.AddWithValue("@DocumentId", documentId);
                                    attachCommand.Parameters.AddWithValue("@FileName", savedFile.FileName);
                                    attachCommand.Parameters.AddWithValue("@OriginalFileName", attachmentFile.FileName);
                                    attachCommand.Parameters.AddWithValue("@FilePath", savedFile.FilePath.Replace(_environment.WebRootPath, "").Replace("\\", "/"));
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", User.FindFirstValue(ClaimTypes.NameIdentifier));
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.PDF);
                                    attachCommand.Parameters.AddWithValue("@DisplayOrder", 1);
                                    attachCommand.Parameters.AddWithValue("@IsPrimaryDocument", true);
                                    attachCommand.Parameters.AddWithValue("@FileHash", savedFile.FileHash);

                                    await attachCommand.ExecuteNonQueryAsync();
                                }

                                // Update HasAttachments flag
                                var updateQuery = "UPDATE Documents SET HasAttachments = 1 WHERE DocumentId = @DocumentId";
                                using (var updateCommand = new SqlCommand(updateQuery, connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@DocumentId", documentId);
                                    await updateCommand.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = $"Dokumenti dalës '{model.ProtocolNumber}' u regjistrua me sukses!";
                            return RedirectToAction(nameof(Index));
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }

            await LoadDropdowns();
            return View(model);
        }

        // GET: Manager/OutgoingDocument/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            OutgoingDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Main document with JOINs
                var query = @"SELECT d.*, 
                    i.Name as InstitutionName, i.ShortName as InstitutionShortName, i.Adress as InstitutionAdress,
                    c.Name as ClassificationName, c.ColorCode,
                    u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentId = @DocumentId AND d.DocumentType = @DocumentType";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            document = DocumentMapper.MapToOutgoingDocument(reader);

                            // Populate Institution
                            if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
                            {
                                document.Institution = new Institution
                                {
                                    InstitutionId = document.InstitutionId,
                                    Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                                    ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("InstitutionShortName")),
                                    Adress = reader.IsDBNull(reader.GetOrdinal("InstitutionAdress"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("InstitutionAdress"))
                                };
                            }

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
                                document.Creator = new Users
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("CreatorUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("CreatorFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("CreatorLastName"))
                                };
                            }
                        }
                    }
                }

                if (document == null) return NotFound();

                // Attachments
                document.Attachments = new List<DocumentAttachment>();
                var attachQuery = "SELECT * FROM DocumentAttachments WHERE DocumentId = @DocumentId ORDER BY DisplayOrder";
                using (var command = new SqlCommand(attachQuery, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            document.Attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));
                        }
                    }
                }

                // Trackings
                document.Trackings = new List<DocumentTracking>();
                var trackQuery = @"SELECT dt.*, 
                    u.UserName as AssignedToUserName, u.FirstName as AssignedToFirstName, u.LastName as AssignedToLastName
                    FROM DocumentTrackings dt
                    LEFT JOIN AspNetUsers u ON dt.AssignedToUserId = u.Id
                    WHERE dt.DocumentId = @DocumentId
                    ORDER BY dt.CreatedDate DESC";
                using (var command = new SqlCommand(trackQuery, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tracking = TrackingMapper.MapToDocumentTracking(reader);
                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToUserName")))
                            {
                                tracking.AssignedToUser = new Users
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedToUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }
                            document.Trackings.Add(tracking);
                        }
                    }
                }

                // Deadlines
                document.Deadlines = new List<Deadline>();
                var deadlineQuery = @"SELECT d.*, 
                    u.UserName as ResponsibleUserName, u.FirstName as ResponsibleFirstName, u.LastName as ResponsibleLastName
                    FROM Deadlines d
                    LEFT JOIN AspNetUsers u ON d.ResponsibleUserId = u.Id
                    WHERE d.DocumentId = @DocumentId
                    ORDER BY d.DueDate";
                using (var command = new SqlCommand(deadlineQuery, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var deadline = DeadlineMapper.MapToDeadline(reader);
                            if (!reader.IsDBNull(reader.GetOrdinal("ResponsibleUserName")))
                            {
                                deadline.ResponsibleUser = new Users
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("ResponsibleUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("ResponsibleFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("ResponsibleLastName"))
                                };
                            }
                            document.Deadlines.Add(deadline);
                        }
                    }
                }
            }

            return View(document);
        }

        // POST: Manager/OutgoingDocument/MarkAsSent/5
        [HttpPost]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE Documents SET
                    Status = @CompletedStatus,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE DocumentId = @DocumentId AND DocumentType = @DocumentType";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
                    command.Parameters.AddWithValue("@CompletedStatus", (int)DocumentStatus.Completed);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System");
                    command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Dokumenti u shënua si i dërguar!" });
                    else
                        return Json(new { success = false, message = "Dokumenti nuk u gjet!" });
                }
            }
        }

        // POST: Manager/OutgoingDocument/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get attachments first
                var attachments = new List<DocumentAttachment>();
                var queryAttach = "SELECT * FROM DocumentAttachments WHERE DocumentId = @DocumentId";
                using (var command = new SqlCommand(queryAttach, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));
                        }
                    }
                }

                // Get protocol number
                string protocolNumber = null;
                var queryProto = "SELECT ProtocolNumber FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = @DocumentType";
                using (var command = new SqlCommand(queryProto, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
                    command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
                    var result = await command.ExecuteScalarAsync();
                    protocolNumber = result?.ToString();
                }

                if (string.IsNullOrEmpty(protocolNumber))
                {
                    TempData["ErrorMessage"] = "Dokumenti nuk u gjet!";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    // Delete physical files
                    foreach (var attachment in attachments)
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    // Delete from database
                    var query = "DELETE FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = @DocumentType";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentId", id);
                        command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
                        await command.ExecuteNonQueryAsync();
                    }

                    TempData["SuccessMessage"] = $"Dokumenti '{protocolNumber}' u fshi me sukses!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Manager/OutgoingDocument/DeleteAttachment
        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                DocumentAttachment attachment = null;
                var query = "SELECT * FROM DocumentAttachments WHERE AttachmentId = @AttachmentId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AttachmentId", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            attachment = AttachmentMapper.MapToDocumentAttachment(reader);
                        }
                    }
                }

                if (attachment == null)
                    return Json(new { success = false, message = "Shtojca nuk u gjet!" });

                try
                {
                    var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    var deleteQuery = "DELETE FROM DocumentAttachments WHERE AttachmentId = @AttachmentId";
                    using (var command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AttachmentId", id);
                        await command.ExecuteNonQueryAsync();
                    }

                    return Json(new { success = true, message = "Shtojca u fshi me sukses!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Gabim: {ex.Message}" });
                }
            }
        }

        // ========== HELPER METHODS ==========
        private async Task LoadDropdowns(SqlConnection? existingConnection = null, int? selectedInstitutionId = null, int? selectedClassificationId = null)
        {
            var shouldCloseConnection = false;
            SqlConnection connection;

            if (existingConnection == null)
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }
            else
            {
                connection = existingConnection;
            }

            try
            {
                // Load Institutions
                var institutions = new List<SelectListItem>();
                var queryInst = "SELECT InstitutionId, Name FROM Institutions WHERE IsActive = 1 ORDER BY Name";
                using (var command = new SqlCommand(queryInst, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            institutions.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }

                ViewBag.Institutions = new SelectList(institutions, "Value", "Text", selectedInstitutionId);

                // Load Classifications
                var classifications = new List<SelectListItem>();
                var queryClass = "SELECT ClassificationId, Name FROM Classifications WHERE IsActive = 1 ORDER BY Name";
                using (var command = new SqlCommand(queryClass, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            classifications.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }

                ViewBag.Classifications = new SelectList(classifications, "Value", "Text", selectedClassificationId);

                // Load Incoming Documents for response dropdown
                var incomingDocs = new List<SelectListItem>();
                var queryIncoming = "SELECT DocumentId, ProtocolNumber, Subject FROM Documents WHERE DocumentType = 1 ORDER BY ProtocolNumber DESC";
                using (var command = new SqlCommand(queryIncoming, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            incomingDocs.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = $"{reader.GetString(1)} - {reader.GetString(2)}"
                            });
                        }
                    }
                }
                ViewBag.IncomingDocuments = new SelectList(incomingDocs, "Value", "Text");
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
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

        private void AddOutgoingDocumentParameters(SqlCommand command, OutgoingDocument model)
        {
            command.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
            command.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
            command.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
            command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
            command.Parameters.AddWithValue("@Subject", model.Subject);
            command.Parameters.AddWithValue("@Content", (object?)model.Content ?? DBNull.Value);
            command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
            command.Parameters.AddWithValue("@Status", (int)model.Status);
            command.Parameters.AddWithValue("@Priority", (int)model.Priority);
            command.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@HasAttachments", false);
            command.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
            command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
            command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
            command.Parameters.AddWithValue("@RecipientName", model.RecipientName);
            command.Parameters.AddWithValue("@IsResponse", model.IsResponse);
            command.Parameters.AddWithValue("@OriginalIncomingDocumentId", (object?)model.OriginalIncomingDocumentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ArchiveLocation", (object?)model.ArchiveLocation ?? DBNull.Value);
        }
    }
}