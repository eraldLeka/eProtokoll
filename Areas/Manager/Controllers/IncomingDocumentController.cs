using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Security.Claims;
using eProtokoll.Services.Files;
using eProtokoll.Services.ProtocolNumber;
using DocumentType = eProtokoll.Models.DocumentType;


namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class IncomingDocumentController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private readonly IProtocolNumberService _protocolNumberService;
        private readonly FileService _fileService;
        public IncomingDocumentController(IConfiguration configuration, IWebHostEnvironment environment, IProtocolNumberService protocolNumberService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
            _protocolNumberService = protocolNumberService;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "incoming");
            _fileService = new FileService(uploadsFolder);
        }

        // GET: Manager/IncomingDocument
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string institution = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var documents = new List<IncomingDocument>();

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
                    WHERE d.DocumentType = 1");

                var parameters = new List<SqlParameter>();

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.SenderName LIKE @SearchTerm 
                        OR d.Content LIKE @SearchTerm)");
                    parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                // Get total count
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentType = 1");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(" AND (d.Subject LIKE @SearchTerm OR d.Content LIKE @SearchTerm OR d.ProtocolNumber LIKE @SearchTerm)");
                }

                int totalItems;
                using (var countCommand = new SqlCommand(countQueryBuilder.ToString(), connection))
                {
                    countCommand.Parameters.AddRange(parameters.ToArray());
                    var result = await countCommand.ExecuteScalarAsync();
                    totalItems = result != null ? Convert.ToInt32(result) : 0;
                }

                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Add sorting and pagination
                queryBuilder.Append(@" ORDER BY d.CreatedDate DESC, d.ReceivedDate DESC
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
                            var document = DocumentMapper.MapToIncomingDocument(reader);

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
                ViewBag.Institutions = institutions;

                // Statistics
                ViewBag.TotalIncoming = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1");

                var queryToday = "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1 AND CAST(ReceivedDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    ViewBag.TodayIncoming = (int)await command.ExecuteScalarAsync();
                }

            }

            return View(documents);
        }

        // GET: Manager/IncomingDocument/Create
        public async Task<IActionResult> Create()
        {
            var protocolNumber = await _protocolNumberService.GenerateNextProtocolNumberAsync(eProtokoll.Services.ProtocolNumber.DocumentType.Incoming);
            var now = DateTime.Now;

            var document = new IncomingDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = now.Date,
                ProtocolTime = new TimeSpan(now.Hour, now.Minute, now.Second),
                ReceivedDate = now.Date,
                Status = DocumentStatus.Registered,
                Priority = Priority.Normal
            };

            await LoadDropdowns();
            return View(document);
        }
        // POST: Manager/IncomingDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomingDocument model, IFormFile? attachmentFile)
        {
            var protocolNumber = await _protocolNumberService.GenerateNextProtocolNumberAsync((Services.ProtocolNumber.DocumentType)DocumentType.Incoming);
            model.ProtocolNumber = protocolNumber;

            var now = DateTime.Now;
            model.ProtocolDate = now.Date;
            model.ProtocolTime = new TimeSpan(now.Hour, now.Minute, now.Second);
            model.ReceivedDate = now.Date;
            model.Status = DocumentStatus.Registered;
            model.Priority = Priority.Normal;


            ModelState.Remove(nameof(model.ProtocolNumber));
            ModelState.Remove(nameof(model.ProtocolDate));
            ModelState.Remove(nameof(model.ProtocolTime));
            ModelState.Remove(nameof(model.ReceivedDate));

            if (attachmentFile == null || attachmentFile.Length == 0)
            {
                ModelState.AddModelError("attachmentFile", "Ngarko pdf per dokumentin hyres.");
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
                model.DocumentType = DocumentType.Incoming;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert IncomingDocument
                            var query = @"
                                    INSERT INTO Documents (
                                    ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                                    ClassificationId, Status, Priority,
                                    Notes, HasAttachments, CreatedDate, CreatedBy,
                                    InstitutionId, SenderName,ReceivedDate,
                                    OriginalDocumentNumber, OriginalDocumentDate, 
                                    Discriminator
                                ) OUTPUT INSERTED.DocumentId VALUES (
                                    @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                                    @ClassificationId, @Status, @Priority,
                                    @Notes,@HasAttachments, @CreatedDate, @CreatedBy,
                                    @InstitutionId, @SenderName, @ReceivedDate,@OriginalDocumentNumber, @OriginalDocumentDate,
                                    @Discriminator
                                )";

                            int documentId;
                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                AddIncomingDocumentParameters(command, model);
                                documentId = (int)await command.ExecuteScalarAsync();
                            }

                            // Handle file upload
                            if (attachmentFile != null && attachmentFile.Length > 0)
                            {
                                using var ms = new MemoryStream();
                                await attachmentFile.CopyToAsync(ms);
                                var fileBytes = ms.ToArray();
                                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                                var savedFile = _fileService.SaveFile(fileBytes, attachmentFile.FileName, documentId, attachmentFile.ContentType, userId);

                                var attachmentQuery = @"
                                   INSERT INTO DocumentAttachments (
                                        DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                                        ContentType, UploadedDate, UploadedBy, Category, DisplayOrder, IsPrimaryDocument, FileHash
                                    ) VALUES (
                                        @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                                        @ContentType, @UploadedDate, @UploadedBy, @Category, @DisplayOrder, @IsPrimaryDocument, @FileHash
                                    )";



                                using (var attachCommand = new SqlCommand(attachmentQuery, connection, transaction))
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
                            TempData["SuccessMessage"] = $"Dokumenti hyrës '{model.ProtocolNumber}' u regjistrua me sukses!";
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

        // GET: Manager/IncomingDocument/Details/
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            IncomingDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Main document with JOINs
                var query = @"SELECT d.*, 
                    i.Name as InstitutionName, i.ShortName as InstitutionShortName, i.Adress as InstitutionAdress,
                    c.Name as ClassificationName, c.ColorCode, c.Level as ClassificationLevel,
                    u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentId = @DocumentId AND d.DocumentType = 1";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            document = DocumentMapper.MapToIncomingDocument(reader);

                            // Institution
                            if (!reader.IsDBNull(reader.GetOrdinal("InstitutionName")))
                            {
                                document.Institution = new Institution
                                {
                                    InstitutionId = document.InstitutionId,
                                    Name = reader.GetString(reader.GetOrdinal("InstitutionName")),
                                    ShortName = reader.IsDBNull(reader.GetOrdinal("InstitutionShortName")) ? null : reader.GetString(reader.GetOrdinal("InstitutionShortName")),
                                    Adress = reader.IsDBNull(reader.GetOrdinal("InstitutionAdress")) ? null : reader.GetString(reader.GetOrdinal("InstitutionAdress"))
                                };
                            }

                            // Classification
                            if (!reader.IsDBNull(reader.GetOrdinal("ClassificationName")))
                            {
                                document.Classification = new Classification
                                {
                                    ClassificationId = document.ClassificationId,
                                    Name = reader.GetString(reader.GetOrdinal("ClassificationName")),
                                    ColorCode = reader.IsDBNull(reader.GetOrdinal("ColorCode")) ? null : reader.GetString(reader.GetOrdinal("ColorCode")),
                                    Level = (AccessLevel)reader.GetInt32(reader.GetOrdinal("ClassificationLevel"))
                                };
                            }

                            // Creator
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

        // POST: Manager/IncomingDocument/Delete/
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

                // Get protocol number for message
                string protocolNumber = "";
                var queryProtocol = "SELECT ProtocolNumber FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 1";
                using (var command = new SqlCommand(queryProtocol, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
                    var result = await command.ExecuteScalarAsync();
                    protocolNumber = result?.ToString() ?? "";
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
                    var query = "DELETE FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 1";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DocumentId", id);
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
        // POST: Manager/IncomingDocument/DeleteAttachment
        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get attachment details
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
                    // Delete physical file
                    var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Delete from database
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
        private async Task LoadDropdowns(int? selectedInstitutionId = null, int? selectedClassificationId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Institutions
                var institutions = new List<Institution>();
                var queryInst = "SELECT * FROM Institutions WHERE IsActive = 1 ORDER BY Name";
                using (var command = new SqlCommand(queryInst, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            institutions.Add(new Institution
                            {
                                InstitutionId = reader.GetInt32(reader.GetOrdinal("InstitutionId")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }

                ViewBag.Institutions = new SelectList(institutions, "InstitutionId", "Name", selectedInstitutionId);

                // Classifications
                var classifications = new List<Classification>();
                var queryClass = "SELECT * FROM Classifications WHERE IsActive = 1 ORDER BY Name";
                using (var command = new SqlCommand(queryClass, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            classifications.Add(new Classification
                            {
                                ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }

                ViewBag.Classifications = new SelectList(classifications, "ClassificationId", "Name", selectedClassificationId);
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
        private void AddIncomingDocumentParameters(SqlCommand command, IncomingDocument model)
        {
            command.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
            command.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
            command.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
            command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Incoming);
            command.Parameters.AddWithValue("@Subject", model.Subject);
            command.Parameters.AddWithValue("@Content", (object)model.Content ?? DBNull.Value);
            command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
            command.Parameters.AddWithValue("@Status", (int)model.Status);
            command.Parameters.AddWithValue("@Priority", (int)model.Priority);
            command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@HasAttachments", false);
            command.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
            command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
            command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
            command.Parameters.AddWithValue("@SenderName", model.SenderName);
            command.Parameters.AddWithValue("@ReceivedDate", model.ReceivedDate);
            command.Parameters.AddWithValue("@OriginalDocumentNumber", (object)model.OriginalDocumentNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@OriginalDocumentDate", (object)model.OriginalDocumentDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Discriminator", "IncomingDocument");
        }
    }
}