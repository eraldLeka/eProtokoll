using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using eProtokoll.Services.Mappers;  // ← SHTUAR!

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class IncomingDocumentController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public IncomingDocumentController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
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

                // Status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var docStatus))
                {
                    queryBuilder.Append(" AND d.Status = @Status");
                    parameters.Add(new SqlParameter("@Status", (int)docStatus));
                }

                // Priority filter
                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var docPriority))
                {
                    queryBuilder.Append(" AND d.Priority = @Priority");
                    parameters.Add(new SqlParameter("@Priority", (int)docPriority));
                }

                // Institution filter
                if (!string.IsNullOrEmpty(institution) && int.TryParse(institution, out var institutionId))
                {
                    queryBuilder.Append(" AND d.InstitutionId = @InstitutionId");
                    parameters.Add(new SqlParameter("@InstitutionId", institutionId));
                }

                // Date range filters
                if (dateFrom.HasValue)
                {
                    queryBuilder.Append(" AND d.ReceivedDate >= @DateFrom");
                    parameters.Add(new SqlParameter("@DateFrom", dateFrom.Value));
                }

                if (dateTo.HasValue)
                {
                    queryBuilder.Append(" AND d.ReceivedDate <= @DateTo");
                    parameters.Add(new SqlParameter("@DateTo", dateTo.Value));
                }

                // Get total count - build separate count query with same WHERE clause
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentType = 1");

                // Apply same filters as main query
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(" AND (d.Subject LIKE @SearchTerm OR d.Content LIKE @SearchTerm OR d.ProtocolNumber LIKE @SearchTerm)");
                }

                if (!string.IsNullOrEmpty(status))
                {
                    countQueryBuilder.Append(" AND d.Status = @Status");
                }

                if (!string.IsNullOrEmpty(priority))
                {
                    countQueryBuilder.Append(" AND d.Priority = @Priority");
                }

                if (!string.IsNullOrEmpty(institution))
                {
                    countQueryBuilder.Append(" AND d.InstitutionId = @InstitutionId");
                }

                if (dateFrom.HasValue)
                {
                    countQueryBuilder.Append(" AND d.ReceivedDate >= @DateFrom");
                }

                if (dateTo.HasValue)
                {
                    countQueryBuilder.Append(" AND d.ReceivedDate <= @DateTo");
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
                            // NDRYSHIMI: Përdor DocumentMapper nga Services
                            var document = DocumentMapper.MapToIncomingDocument(reader);  // ← NDRYSHUAR!

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

                // ViewBag for filters
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SelectedStatus = status;
                ViewBag.SelectedPriority = priority;
                ViewBag.SelectedInstitution = institution;
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
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

                var queryNeedsResponse = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = 1 AND RequiresResponse = 1 AND IsResponded = 0";
                ViewBag.NeedsResponse = await ExecuteCountQuery(connection, queryNeedsResponse);

                var queryOverdue = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = 1 
                    AND RequiresResponse = 1 
                    AND IsResponded = 0 
                    AND ResponseDeadline IS NOT NULL 
                    AND ResponseDeadline < @Now";
                using (var command = new SqlCommand(queryOverdue, connection))
                {
                    command.Parameters.AddWithValue("@Now", DateTime.Now);
                    ViewBag.Overdue = (int)await command.ExecuteScalarAsync();
                }
            }

            return View(documents);
        }

        // GET: Manager/IncomingDocument/Create
        public async Task<IActionResult> Create()
        {
            var protocolNumber = await GenerateProtocolNumber();
            var now = DateTime.Now;
            var currentTime = new TimeSpan(now.Hour, now.Minute, now.Second);

            var document = new IncomingDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = DateTime.Now.Date,
                ProtocolTime = currentTime,
                ReceivedDate = DateTime.Now.Date,
                ReceivedTime = currentTime,
                Status = DocumentStatus.Registered,
                Priority = Priority.Normal,
                Language = "Shqip",
                HasPhysicalCopy = true,
                DeliveryMethod = DeliveryMethod.HandDelivery
            };

            await LoadDropdowns();
            return View(document);
        }

        // POST: Manager/IncomingDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomingDocument model, IFormFile? attachmentFile)
        {
            if (ModelState.IsValid)
            {
                // Set metadata
                model.CreatedDate = DateTime.Now;
                model.CreatedBy = User.Identity.Name;
                model.DocumentType = DocumentType.Incoming;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert IncomingDocument
                            var query = @"INSERT INTO Documents (
                                ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                                ReferenceNumber, ReferenceDate, ClassificationId, Status, Priority,
                                HasDeadline, DeadlineDate, Notes, PageCount, Language, IsScanned,
                                HasAttachments, IsArchived, ArchivedDate, ArchivedBy, CreatedDate, CreatedBy,
                                ModifiedDate, ModifiedBy, InstitutionId, SenderName, SenderPosition,
                                SenderEmail, SenderPhone, ReceivedDate, ReceivedTime, DeliveryMethod,
                                OriginalDocumentNumber, OriginalDocumentDate, RequiresResponse,
                                ResponseDeadline, IsResponded, ResponseDate, HasPhysicalCopy, PhysicalLocation
                            ) OUTPUT INSERTED.DocumentId VALUES (
                                @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                                @ReferenceNumber, @ReferenceDate, @ClassificationId, @Status, @Priority,
                                @HasDeadline, @DeadlineDate, @Notes, @PageCount, @Language, @IsScanned,
                                @HasAttachments, @IsArchived, @ArchivedDate, @ArchivedBy, @CreatedDate, @CreatedBy,
                                @ModifiedDate, @ModifiedBy, @InstitutionId, @SenderName, @SenderPosition,
                                @SenderEmail, @SenderPhone, @ReceivedDate, @ReceivedTime, @DeliveryMethod,
                                @OriginalDocumentNumber, @OriginalDocumentDate, @RequiresResponse,
                                @ResponseDeadline, @IsResponded, @ResponseDate, @HasPhysicalCopy, @PhysicalLocation
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
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "incoming");
                                Directory.CreateDirectory(uploadsFolder);

                                var uniqueFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await attachmentFile.CopyToAsync(fileStream);
                                }

                                // Insert attachment
                                var attachmentQuery = @"INSERT INTO DocumentAttachments (
                                    DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                                    ContentType, UploadedDate, UploadedBy, Category, IsVirusScanned,
                                    AllowDownload, AllowPrint, DisplayOrder, IsPrimaryDocument
                                ) VALUES (
                                    @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                                    @ContentType, @UploadedDate, @UploadedBy, @Category, @IsVirusScanned,
                                    @AllowDownload, @AllowPrint, @DisplayOrder, @IsPrimaryDocument
                                )";

                                using (var attachCommand = new SqlCommand(attachmentQuery, connection, transaction))
                                {
                                    attachCommand.Parameters.AddWithValue("@DocumentId", documentId);
                                    attachCommand.Parameters.AddWithValue("@FileName", uniqueFileName);
                                    attachCommand.Parameters.AddWithValue("@OriginalFileName", attachmentFile.FileName);
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/incoming/{uniqueFileName}");
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", User.Identity.Name);
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
                                    attachCommand.Parameters.AddWithValue("@IsVirusScanned", false);
                                    attachCommand.Parameters.AddWithValue("@AllowDownload", true);
                                    attachCommand.Parameters.AddWithValue("@AllowPrint", true);
                                    attachCommand.Parameters.AddWithValue("@DisplayOrder", 1);
                                    attachCommand.Parameters.AddWithValue("@IsPrimaryDocument", true);

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

        // GET: Manager/IncomingDocument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            IncomingDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 1";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // NDRYSHIMI: Përdor DocumentMapper nga Services
                            document = DocumentMapper.MapToIncomingDocument(reader);  // ← NDRYSHUAR!
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
                                // NDRYSHIMI: Përdor AttachmentMapper nga Services
                                document.Attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));  // ← NDRYSHUAR!
                            }
                        }
                    }
                }
            }

            if (document == null) return NotFound();

            await LoadDropdowns(document.InstitutionId, document.ClassificationId);
            return View(document);
        }

        // POST: Manager/IncomingDocument/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IncomingDocument model, IFormFile? attachmentFile)
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
                            // Update IncomingDocument
                            var query = @"UPDATE Documents SET
                                InstitutionId = @InstitutionId,
                                SenderName = @SenderName,
                                SenderPosition = @SenderPosition,
                                SenderEmail = @SenderEmail,
                                SenderPhone = @SenderPhone,
                                Subject = @Subject,
                                Content = @Content,
                                ReceivedDate = @ReceivedDate,
                                ReceivedTime = @ReceivedTime,
                                DeliveryMethod = @DeliveryMethod,
                                OriginalDocumentNumber = @OriginalDocumentNumber,
                                OriginalDocumentDate = @OriginalDocumentDate,
                                ClassificationId = @ClassificationId,
                                Status = @Status,
                                Priority = @Priority,
                                RequiresResponse = @RequiresResponse,
                                ResponseDeadline = @ResponseDeadline,
                                HasPhysicalCopy = @HasPhysicalCopy,
                                PhysicalLocation = @PhysicalLocation,
                                Notes = @Notes,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                                WHERE DocumentId = @DocumentId AND DocumentType = 1";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", id);
                                command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
                                command.Parameters.AddWithValue("@SenderName", model.SenderName);
                                command.Parameters.AddWithValue("@SenderPosition", (object)model.SenderPosition ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SenderEmail", (object)model.SenderEmail ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SenderPhone", (object)model.SenderPhone ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Subject", model.Subject);
                                command.Parameters.AddWithValue("@Content", (object)model.Content ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ReceivedDate", model.ReceivedDate);
                                command.Parameters.AddWithValue("@ReceivedTime", model.ReceivedTime);
                                command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
                                command.Parameters.AddWithValue("@OriginalDocumentNumber", (object)model.OriginalDocumentNumber ?? DBNull.Value);
                                command.Parameters.AddWithValue("@OriginalDocumentDate", (object)model.OriginalDocumentDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
                                command.Parameters.AddWithValue("@Status", (int)model.Status);
                                command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                                command.Parameters.AddWithValue("@RequiresResponse", model.RequiresResponse);
                                command.Parameters.AddWithValue("@ResponseDeadline", (object)model.ResponseDeadline ?? DBNull.Value);
                                command.Parameters.AddWithValue("@HasPhysicalCopy", model.HasPhysicalCopy);
                                command.Parameters.AddWithValue("@PhysicalLocation", (object)model.PhysicalLocation ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                                command.Parameters.AddWithValue("@ModifiedBy", (object)User.Identity?.Name ?? DBNull.Value);

                                await command.ExecuteNonQueryAsync();
                            }

                            // Handle new file upload
                            if (attachmentFile != null && attachmentFile.Length > 0)
                            {
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "incoming");
                                Directory.CreateDirectory(uploadsFolder);

                                var uniqueFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await attachmentFile.CopyToAsync(fileStream);
                                }

                                // Get current max DisplayOrder
                                var orderQuery = "SELECT ISNULL(MAX(DisplayOrder), 0) FROM DocumentAttachments WHERE DocumentId = @DocumentId";
                                int maxOrder;
                                using (var orderCommand = new SqlCommand(orderQuery, connection, transaction))
                                {
                                    orderCommand.Parameters.AddWithValue("@DocumentId", id);
                                    maxOrder = (int)await orderCommand.ExecuteScalarAsync();
                                }

                                // Insert attachment
                                var attachmentQuery = @"INSERT INTO DocumentAttachments (
                                    DocumentId, FileName, OriginalFileName, FilePath, FileSize, FileExtension,
                                    ContentType, UploadedDate, UploadedBy, Category, IsVirusScanned,
                                    AllowDownload, AllowPrint, DisplayOrder
                                ) VALUES (
                                    @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
                                    @ContentType, @UploadedDate, @UploadedBy, @Category, @IsVirusScanned,
                                    @AllowDownload, @AllowPrint, @DisplayOrder
                                )";

                                using (var attachCommand = new SqlCommand(attachmentQuery, connection, transaction))
                                {
                                    attachCommand.Parameters.AddWithValue("@DocumentId", id);
                                    attachCommand.Parameters.AddWithValue("@FileName", uniqueFileName);
                                    attachCommand.Parameters.AddWithValue("@OriginalFileName", attachmentFile.FileName);
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/incoming/{uniqueFileName}");
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", User.Identity?.Name ?? "System");
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
                                    attachCommand.Parameters.AddWithValue("@IsVirusScanned", false);
                                    attachCommand.Parameters.AddWithValue("@AllowDownload", true);
                                    attachCommand.Parameters.AddWithValue("@AllowPrint", true);
                                    attachCommand.Parameters.AddWithValue("@DisplayOrder", maxOrder + 1);

                                    await attachCommand.ExecuteNonQueryAsync();
                                }

                                // Update HasAttachments flag
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
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }

            await LoadDropdowns(model.InstitutionId, model.ClassificationId);
            return View(model);
        }

        // GET: Manager/IncomingDocument/Details/5
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
                            // NDRYSHIMI: Përdor DocumentMapper nga Services
                            document = DocumentMapper.MapToIncomingDocument(reader);  // ← NDRYSHUAR!

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
                            // NDRYSHIMI: Përdor AttachmentMapper nga Services
                            document.Attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));  // ← NDRYSHUAR!
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
                            // NDRYSHIMI: Përdor TrackingMapper nga Services
                            var tracking = TrackingMapper.MapToDocumentTracking(reader);  // ← NDRYSHUAR!
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
                            // NDRYSHIMI: Përdor DeadlineMapper nga Services
                            var deadline = DeadlineMapper.MapToDeadline(reader);  // ← NDRYSHUAR!
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

        // POST: Manager/IncomingDocument/Delete/5
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
                            // NDRYSHIMI: Përdor AttachmentMapper nga Services
                            attachments.Add(AttachmentMapper.MapToDocumentAttachment(reader));  // ← NDRYSHUAR!
                        }
                    }
                }

                // Get protocol number for message
                string protocolNumber = "";
                var queryProtocol = "SELECT ProtocolNumber FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 0";
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
                            // NDRYSHIMI: Përdor AttachmentMapper nga Services
                            attachment = AttachmentMapper.MapToDocumentAttachment(reader);  // ← NDRYSHUAR!
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

        // ========== HELPER METHODS (mbeten këtu - nuk janë mapping!) ==========

        private async Task<string> GenerateProtocolNumber()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var currentYear = DateTime.Now.Year;
                        ProtocolSettings settings = null;

                        // Get settings
                        var query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = 1";
                        using (var command = new SqlCommand(query, connection, transaction))
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    // NDRYSHIMI: Përdor ProtocolSettingsMapper nga Services
                                    settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);  // ← NDRYSHUAR!
                                }
                            }
                        }

                        // Create default settings if not exists
                        if (settings == null)
                        {
                            var insertQuery = @"INSERT INTO ProtocolSettings (
                                Year, IncomingPrefix, IncomingStartNumber, IncomingCurrentNumber,
                                OutgoingPrefix, OutgoingStartNumber, OutgoingCurrentNumber,
                                InternalPrefix, InternalStartNumber, InternalCurrentNumber,
                                ProtocolNumberFormat, NumberPadding, AutoResetYearly,
                                ShowYearInNumber, UseSeparatorSlash, IsActive
                            ) VALUES (
                                @Year, @IncomingPrefix, @IncomingStartNumber, @IncomingCurrentNumber,
                                @OutgoingPrefix, @OutgoingStartNumber, @OutgoingCurrentNumber,
                                @InternalPrefix, @InternalStartNumber, @InternalCurrentNumber,
                                @ProtocolNumberFormat, @NumberPadding, @AutoResetYearly,
                                @ShowYearInNumber, @UseSeparatorSlash, @IsActive
                            )";

                            using (var command = new SqlCommand(insertQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Year", currentYear);
                                command.Parameters.AddWithValue("@IncomingPrefix", "H");
                                command.Parameters.AddWithValue("@IncomingStartNumber", 1);
                                command.Parameters.AddWithValue("@IncomingCurrentNumber", 0);
                                command.Parameters.AddWithValue("@OutgoingPrefix", "D");
                                command.Parameters.AddWithValue("@OutgoingStartNumber", 1);
                                command.Parameters.AddWithValue("@OutgoingCurrentNumber", 0);
                                command.Parameters.AddWithValue("@InternalPrefix", "B");
                                command.Parameters.AddWithValue("@InternalStartNumber", 1);
                                command.Parameters.AddWithValue("@InternalCurrentNumber", 0);
                                command.Parameters.AddWithValue("@ProtocolNumberFormat", "{PREFIX}-{NUMBER}/{YEAR}");
                                command.Parameters.AddWithValue("@NumberPadding", 4);
                                command.Parameters.AddWithValue("@AutoResetYearly", true);
                                command.Parameters.AddWithValue("@ShowYearInNumber", true);
                                command.Parameters.AddWithValue("@UseSeparatorSlash", true);
                                command.Parameters.AddWithValue("@IsActive", true);

                                await command.ExecuteNonQueryAsync();
                            }

                            settings = new ProtocolSettings
                            {
                                Year = currentYear,
                                IncomingPrefix = "H",
                                IncomingCurrentNumber = 0,
                                IncomingStartNumber = 1,
                                ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                                NumberPadding = 4,
                                ShowYearInNumber = true
                            };
                        }

                        // Check for yearly reset
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
                                command.Parameters.AddWithValue("@ResetNumber", settings.IncomingStartNumber - 1);
                                await command.ExecuteNonQueryAsync();
                            }

                            settings.Year = currentYear;
                            settings.IncomingCurrentNumber = settings.IncomingStartNumber - 1;
                        }

                        // Increment number
                        var updateQuery = @"UPDATE ProtocolSettings SET 
                            IncomingCurrentNumber = IncomingCurrentNumber + 1 
                            WHERE ProtocolSettingsId = 1";

                        using (var command = new SqlCommand(updateQuery, connection, transaction))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        settings.IncomingCurrentNumber++;

                        // Generate protocol number
                        var number = settings.IncomingCurrentNumber.ToString(new string('0', settings.NumberPadding));
                        var protocolNumber = settings.ProtocolNumberFormat
                            .Replace("{PREFIX}", settings.IncomingPrefix ?? "H")
                            .Replace("{NUMBER}", number)
                            .Replace("{YEAR}", settings.ShowYearInNumber ? currentYear.ToString() : "")
                            .Replace("{SUFFIX}", settings.IncomingSuffix ?? "");

                        protocolNumber = protocolNumber.Replace("//", "/").Replace("--", "-").Trim('-', '/');

                        transaction.Commit();
                        return protocolNumber;
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
            command.Parameters.AddWithValue("@ReferenceNumber", (object)model.ReferenceNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReferenceDate", (object)model.ReferenceDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
            command.Parameters.AddWithValue("@Status", (int)model.Status);
            command.Parameters.AddWithValue("@Priority", (int)model.Priority);
            command.Parameters.AddWithValue("@HasDeadline", model.HasDeadline);
            command.Parameters.AddWithValue("@DeadlineDate", (object)model.DeadlineDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@PageCount", (object)model.PageCount ?? DBNull.Value);
            command.Parameters.AddWithValue("@Language", (object)model.Language ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsScanned", model.IsScanned);
            command.Parameters.AddWithValue("@HasAttachments", false);
            command.Parameters.AddWithValue("@IsArchived", model.IsArchived);
            command.Parameters.AddWithValue("@ArchivedDate", (object)model.ArchivedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ArchivedBy", (object)model.ArchivedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
            command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
            command.Parameters.AddWithValue("@ModifiedDate", (object)model.ModifiedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedBy", (object)model.ModifiedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
            command.Parameters.AddWithValue("@SenderName", model.SenderName);
            command.Parameters.AddWithValue("@SenderPosition", (object)model.SenderPosition ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenderEmail", (object)model.SenderEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@SenderPhone", (object)model.SenderPhone ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceivedDate", model.ReceivedDate);
            command.Parameters.AddWithValue("@ReceivedTime", model.ReceivedTime);
            command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
            command.Parameters.AddWithValue("@OriginalDocumentNumber", (object)model.OriginalDocumentNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@OriginalDocumentDate", (object)model.OriginalDocumentDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequiresResponse", model.RequiresResponse);
            command.Parameters.AddWithValue("@ResponseDeadline", (object)model.ResponseDeadline ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsResponded", model.IsResponded);
            command.Parameters.AddWithValue("@ResponseDate", (object)model.ResponseDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@HasPhysicalCopy", model.HasPhysicalCopy);
            command.Parameters.AddWithValue("@PhysicalLocation", (object)model.PhysicalLocation ?? DBNull.Value);
        }

        // ❌ FSHIHEN TË GJITHA MAPPING METHODS - TANI NË SERVICES/MAPPERS/MAPPERS.CS:
        // - MapToIncomingDocument
        // - MapToDocumentAttachment
        // - MapToDocumentTracking
        // - MapToDeadline
        // - MapToProtocolSettings
    }
}