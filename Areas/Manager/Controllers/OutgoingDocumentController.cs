using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class OutgoingDocumentController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public OutgoingDocumentController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
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

                // Apply only simple searchTerm filter to count query
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
                                document.Creator = new ApplicationUser
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

                // ViewBag for search and paging (only simple search supported)
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
                ViewBag.TotalOutgoing = await ExecuteCountQuery(connection,
                    "SELECT COUNT(*) FROM Documents WHERE DocumentType = " + (int)DocumentType.Outgoing);

                var queryToday = "SELECT COUNT(*) FROM Documents WHERE DocumentType = @DocType AND CAST(ProtocolDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@DocType", (int)DocumentType.Outgoing);
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    ViewBag.TodayOutgoing = (int)await command.ExecuteScalarAsync();
                }

                var queryInProgress = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = @DocType AND Status IN (@Registered, @InProgress)";
                using (var command = new SqlCommand(queryInProgress, connection))
                {
                    command.Parameters.AddWithValue("@DocType", (int)DocumentType.Outgoing);
                    command.Parameters.AddWithValue("@Registered", (int)DocumentStatus.Registered);
                    command.Parameters.AddWithValue("@InProgress", (int)DocumentStatus.InProgress);
                    ViewBag.InProgress = (int)await command.ExecuteScalarAsync();
                }

                var queryCompleted = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = @DocType AND Status = @Completed";
                using (var command = new SqlCommand(queryCompleted, connection))
                {
                    command.Parameters.AddWithValue("@DocType", (int)DocumentType.Outgoing);
                    command.Parameters.AddWithValue("@Completed", (int)DocumentStatus.Completed);
                    ViewBag.Completed = (int)await command.ExecuteScalarAsync();
                }
            }

            return View(documents);
        }

        // GET: Manager/OutgoingDocument/Create
        public async Task<IActionResult> Create()
        {
            var protocolNumber = await GenerateProtocolNumber();
            var now = DateTime.Now;
            var currentTime = new TimeSpan(now.Hour, now.Minute, now.Second);

            var document = new OutgoingDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = DateTime.Now.Date,
                ProtocolTime = currentTime,
                Status = DocumentStatus.Registered,
                Priority = Priority.Normal,
                HasArchiveCopy = true,
                DeliveryMethod = DeliveryMethod.Email
            };

            await LoadDropdowns();
            return View(document);
        }

        // POST: Manager/OutgoingDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OutgoingDocument model, IFormFile? attachmentFile)
        {
            if (ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                            if (string.IsNullOrEmpty(userId))
                            {
                                ModelState.AddModelError("", "Përdoruesi nuk është i loguar.");
                                await LoadDropdowns();
                                return View(model);
                            }

                            model.CreatedBy = userId;
                            model.CreatedDate = DateTime.Now;
                            model.DocumentType = DocumentType.Outgoing;
                            var query = @"INSERT INTO Documents (
                                ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                                ClassificationId, Status, Priority,
                                HasDeadline, DeadlineDate, Notes, PageCount, Language,
                                HasAttachments, IsArchived, ArchivedDate, ArchivedBy, CreatedDate, CreatedBy,
                                ModifiedDate, ModifiedBy, InstitutionId, RecipientName, RecipientEmail, 
                                DeliveryMethod, IsResponse, OriginalIncomingDocumentId, HasArchiveCopy, 
                                ArchiveLocation, RequiresResponse, Discriminator
                            ) OUTPUT INSERTED.DocumentId VALUES (
                                @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                                @ClassificationId, @Status, @Priority,
                                @HasDeadline, @DeadlineDate, @Notes, @PageCount, @Language,
                                @HasAttachments, @IsArchived, @ArchivedDate, @ArchivedBy, @CreatedDate, @CreatedBy,
                                @ModifiedDate, @ModifiedBy, @InstitutionId, @RecipientName, @RecipientEmail,
                                @DeliveryMethod, @IsResponse, @OriginalIncomingDocumentId, @HasArchiveCopy,
                                @ArchiveLocation, @RequiresResponse, 'OutgoingDocument'
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
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "outgoing");
                                Directory.CreateDirectory(uploadsFolder);

                                var uniqueFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await attachmentFile.CopyToAsync(fileStream);
                                }

                                var attachmentQuery = @"INSERT INTO DocumentAttachments (
                                    DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                                    ContentType, UploadedDate, UploadedBy, Category, DisplayOrder, IsPrimaryDocument
                                ) VALUES (
                                    @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                                    @ContentType, @UploadedDate, @UploadedBy, @Category, @DisplayOrder, @IsPrimaryDocument
                                )";

                                using (var attachCommand = new SqlCommand(attachmentQuery, connection, transaction))
                                {
                                    attachCommand.Parameters.AddWithValue("@DocumentId", documentId);
                                    attachCommand.Parameters.AddWithValue("@FileName", uniqueFileName);
                                    attachCommand.Parameters.AddWithValue("@OriginalFileName", attachmentFile.FileName);
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/outgoing/{uniqueFileName}");
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", userId);
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
                                    attachCommand.Parameters.AddWithValue("@DisplayOrder", 1);
                                    attachCommand.Parameters.AddWithValue("@IsPrimaryDocument", true);
                                    await attachCommand.ExecuteNonQueryAsync();
                                }

                                // Update HasAttachments
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
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = $"Gabim: {ex.Message}";
                        }
                    }
                }
            }

            await LoadDropdowns();
            return View(model);
        }

        // GET: Manager/OutgoingDocument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            OutgoingDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = @DocumentType";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            document = DocumentMapper.MapToOutgoingDocument(reader);
                        }
                    }
                }

                if (document != null)
                {
                    // Load attachments
                    document.Attachments = new List<DocumentAttachment>();
                    var attachQuery = "SELECT * FROM DocumentAttachments WHERE DocumentId = @DocumentId";
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
                }
            }

            if (document == null) return NotFound();

            await LoadDropdowns(document.InstitutionId, document.ClassificationId);
            return View(document);
        }

        // POST: Manager/OutgoingDocument/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OutgoingDocument model, IFormFile? attachmentFile)
        {
            if (id != model.DocumentId) return NotFound();

            if (ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                            var query = @"UPDATE Documents SET
                                InstitutionId = @InstitutionId,
                                RecipientName = @RecipientName,
                                RecipientEmail = @RecipientEmail,
                                Subject = @Subject,
                                Content = @Content,
                                DeliveryMethod = @DeliveryMethod,
                                ClassificationId = @ClassificationId,
                                Status = @Status,
                                Priority = @Priority,
                                HasDeadline = @HasDeadline,
                                DeadlineDate = @DeadlineDate,
                                HasArchiveCopy = @HasArchiveCopy,
                                ArchiveLocation = @ArchiveLocation,
                                Notes = @Notes,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy,
                                IsResponse = @IsResponse,
                                OriginalIncomingDocumentId = @OriginalIncomingDocumentId
                                WHERE DocumentId = @DocumentId AND DocumentType = @DocumentType";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", id);
                                command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
                                command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
                                command.Parameters.AddWithValue("@RecipientName", model.RecipientName);
                                command.Parameters.AddWithValue("@RecipientEmail", (object?)model.RecipientEmail ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Subject", model.Subject);
                                command.Parameters.AddWithValue("@Content", (object?)model.Content ?? DBNull.Value);
                                command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
                                command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
                                command.Parameters.AddWithValue("@Status", (int)model.Status);
                                command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                                command.Parameters.AddWithValue("@HasDeadline", model.HasDeadline);
                                command.Parameters.AddWithValue("@DeadlineDate", (object?)model.DeadlineDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@HasArchiveCopy", model.HasArchiveCopy);
                                command.Parameters.AddWithValue("@ArchiveLocation", (object?)model.ArchiveLocation ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                                command.Parameters.AddWithValue("@ModifiedBy", userId);
                                command.Parameters.AddWithValue("@IsResponse", model.IsResponse);
                                command.Parameters.AddWithValue("@OriginalIncomingDocumentId", (object?)model.OriginalIncomingDocumentId ?? DBNull.Value);

                                await command.ExecuteNonQueryAsync();
                            }

                            // Handle new file upload
                            if (attachmentFile != null && attachmentFile.Length > 0)
                            {
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "outgoing");
                                Directory.CreateDirectory(uploadsFolder);

                                var uniqueFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await attachmentFile.CopyToAsync(fileStream);
                                }

                                // Get current attachment count
                                var countQuery = "SELECT COUNT(*) FROM DocumentAttachments WHERE DocumentId = @DocumentId";
                                int attachmentCount;
                                using (var countCommand = new SqlCommand(countQuery, connection, transaction))
                                {
                                    countCommand.Parameters.AddWithValue("@DocumentId", id);
                                    attachmentCount = (int)await countCommand.ExecuteScalarAsync();
                                }

                                // Insert attachment
                                var attachmentQuery = @"INSERT INTO DocumentAttachments (
                                    DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                                    ContentType, UploadedDate, UploadedBy, Category, DisplayOrder
                                ) VALUES (
                                    @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                                    @ContentType, @UploadedDate, @UploadedBy, @Category, @DisplayOrder
                                )";

                                using (var attachCommand = new SqlCommand(attachmentQuery, connection, transaction))
                                {
                                    attachCommand.Parameters.AddWithValue("@DocumentId", id);
                                    attachCommand.Parameters.AddWithValue("@FileName", uniqueFileName);
                                    attachCommand.Parameters.AddWithValue("@OriginalFileName", attachmentFile.FileName);
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/outgoing/{uniqueFileName}");
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", userId);
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
                                    attachCommand.Parameters.AddWithValue("@DisplayOrder", attachmentCount + 1);
                                    await attachCommand.ExecuteNonQueryAsync();
                                }

                                // Update HasAttachments
                                var updateQuery = "UPDATE Documents SET HasAttachments = 1 WHERE DocumentId = @DocumentId";
                                using (var updateCommand = new SqlCommand(updateQuery, connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@DocumentId", id);
                                    await updateCommand.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = $"Dokumenti '{model.ProtocolNumber}' u përditësua me sukses!";
                            return RedirectToAction(nameof(Index));
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = $"Gabim: {ex.Message}";
                        }
                    }
                }
            }

            await LoadDropdowns(model.InstitutionId, model.ClassificationId);
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
                                document.Creator = new ApplicationUser
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
                                tracking.AssignedToUser = new ApplicationUser
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
                                deadline.ResponsibleUser = new ApplicationUser
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
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
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

        private async Task<string> GenerateProtocolNumber()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        ProtocolSettings settings = null;
                        var currentYear = DateTime.Now.Year;

                        // Get settings
                        var query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = 1";
                        using (var command = new SqlCommand(query, connection, transaction))
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);
                                }
                            }
                        }

                        if (settings == null)
                        {
                            // Create default settings
                            var insertQuery = @"INSERT INTO ProtocolSettings (
                                Year, IncomingPrefix, IncomingStartNumber, IncomingCurrentNumber,
                                OutgoingPrefix, OutgoingStartNumber, OutgoingCurrentNumber,
                                InternalPrefix, InternalStartNumber, InternalCurrentNumber,
                                ProtocolNumberFormat, NumberPadding, AutoResetYearly, ShowYearInNumber,
                                UseSeparatorSlash, IsActive, CreatedDate
                            ) VALUES (
                                @Year, 'H', 1, 0, 'D', 1, 0, 'B', 1, 0,
                                '{PREFIX}-{NUMBER}/{YEAR}', 4, 1, 1, 1, 1, GETDATE()
                            )";

                            using (var command = new SqlCommand(insertQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Year", currentYear);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Reload settings
                            query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = 1";
                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);
                                    }
                                }
                            }
                        }

                        // Check for year reset
                        if (settings.AutoResetYearly && settings.Year != currentYear)
                        {
                            var resetQuery = @"UPDATE ProtocolSettings SET
                                Year = @Year,
                                IncomingCurrentNumber = @ResetNumber,
                                OutgoingCurrentNumber = @ResetNumber,
                                InternalCurrentNumber = @ResetNumber
                                WHERE ProtocolSettingsId = 1";

                            using (var command = new SqlCommand(resetQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Year", currentYear);
                                command.Parameters.AddWithValue("@ResetNumber", settings.OutgoingStartNumber - 1);
                                await command.ExecuteNonQueryAsync();
                            }

                            settings.Year = currentYear;
                            settings.OutgoingCurrentNumber = settings.OutgoingStartNumber - 1;
                        }

                        // Increment number
                        var updateQuery = @"UPDATE ProtocolSettings SET 
                            OutgoingCurrentNumber = OutgoingCurrentNumber + 1 
                            WHERE ProtocolSettingsId = 1";

                        using (var command = new SqlCommand(updateQuery, connection, transaction))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        settings.OutgoingCurrentNumber++;

                        transaction.Commit();

                        // Generate protocol number
                        var number = settings.OutgoingCurrentNumber.ToString(new string('0', settings.NumberPadding));

                        var protocolNumber = settings.ProtocolNumberFormat
                            .Replace("{PREFIX}", settings.OutgoingPrefix ?? "D")
                            .Replace("{NUMBER}", number)
                            .Replace("{YEAR}", settings.ShowYearInNumber ? currentYear.ToString() : "")
                            .Replace("{SUFFIX}", settings.OutgoingSuffix ?? "");

                        return protocolNumber.Replace("//", "/").Replace("--", "-").Trim('-', '/');
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private async Task LoadDropdowns(int? selectedInstitutionId = null, int? selectedClassificationId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Load Institutions
                var institutions = new List<Institution>();
                var queryInst = "SELECT InstitutionId, Name FROM Institutions WHERE IsActive = 1 ORDER BY Name";
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

                // Load Classifications
                var classifications = new List<Classification>();
                var queryClass = "SELECT ClassificationId, Name FROM Classifications WHERE IsActive = 1 ORDER BY Name";
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

        private void AddOutgoingDocumentParameters(SqlCommand command, OutgoingDocument model)
        {
            // === IDENTITET & PROTOKOLL ===
            command.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
            command.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
            command.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
            command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);

            // === PËRMBAJTJA ===
            command.Parameters.AddWithValue("@Subject", model.Subject);
            command.Parameters.AddWithValue("@Content", (object?)model.Content ?? DBNull.Value);
            command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
            command.Parameters.AddWithValue("@Status", (int)model.Status);
            command.Parameters.AddWithValue("@Priority", (int)model.Priority);

            // === AFATE / META ===
            command.Parameters.AddWithValue("@HasDeadline", model.HasDeadline);
            command.Parameters.AddWithValue("@DeadlineDate", (object?)model.DeadlineDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@HasAttachments", false);
            command.Parameters.AddWithValue("@IsArchived", model.IsArchived);
            command.Parameters.AddWithValue("@ArchivedDate", (object?)model.ArchivedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ArchivedBy", (object?)model.ArchivedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
            command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
            command.Parameters.AddWithValue("@ModifiedDate", (object?)model.ModifiedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedBy", (object?)model.ModifiedBy ?? DBNull.Value);

            // === OUTGOING SPECIFIC (modeli minimal) ===
            command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
            command.Parameters.AddWithValue("@RecipientName", model.RecipientName);
            command.Parameters.AddWithValue("@RecipientEmail", (object?)model.RecipientEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
            command.Parameters.AddWithValue("@IsResponse", model.IsResponse);
            command.Parameters.AddWithValue("@OriginalIncomingDocumentId", (object?)model.OriginalIncomingDocumentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@HasArchiveCopy", model.HasArchiveCopy);
            command.Parameters.AddWithValue("@ArchiveLocation", (object?)model.ArchiveLocation ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequiresResponse", model.IsResponse);
        }
    }
}