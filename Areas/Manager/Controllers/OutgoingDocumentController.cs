using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using eProtokoll.Services.Mappers;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
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
                    WHERE d.DocumentType = 2");

                var parameters = new List<SqlParameter>();

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.RecipientName LIKE @SearchTerm
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
                    queryBuilder.Append(" AND d.SentDate >= @DateFrom");
                    parameters.Add(new SqlParameter("@DateFrom", dateFrom.Value));
                }

                if (dateTo.HasValue)
                {
                    queryBuilder.Append(" AND d.SentDate <= @DateTo");
                    parameters.Add(new SqlParameter("@DateTo", dateTo.Value));
                }

                // Get total count - build separate count query
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentType = 2");

                // Apply same filters as main query
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.RecipientName LIKE @SearchTerm
                        OR d.Content LIKE @SearchTerm)");
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var _))
                {
                    countQueryBuilder.Append(" AND d.Status = @Status");
                }

                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var _))
                {
                    countQueryBuilder.Append(" AND d.Priority = @Priority");
                }

                if (!string.IsNullOrEmpty(institution) && int.TryParse(institution, out var _))
                {
                    countQueryBuilder.Append(" AND d.InstitutionId = @InstitutionId");
                }

                if (dateFrom.HasValue)
                {
                    countQueryBuilder.Append(" AND d.SentDate >= @DateFrom");
                }

                if (dateTo.HasValue)
                {
                    countQueryBuilder.Append(" AND d.SentDate <= @DateTo");
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
                queryBuilder.Append(@" ORDER BY d.CreatedDate DESC, d.SentDate DESC
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
                ViewBag.TotalOutgoing = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2");

                var queryToday = "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2 AND CAST(SentDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    ViewBag.TodayOutgoing = (int)await command.ExecuteScalarAsync();
                }

                var queryPendingSend = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = 2 AND (Status = 4 OR Status = 1)";
                ViewBag.PendingSend = await ExecuteCountQuery(connection, queryPendingSend);

                var querySent = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = 2 AND ShipmentStatus = 3";
                ViewBag.Sent = await ExecuteCountQuery(connection, querySent);
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
                SentDate = DateTime.Now.Date,
                SentTime = currentTime,
                Status = DocumentStatus.Draft,
                Priority = Priority.Normal,
                Language = "Shqip",
                HasArchiveCopy = true,
                DeliveryMethod = DeliveryMethod.HandDelivery,
                ShipmentStatus = ShipmentStatus.Prepared,
                NumberOfCopies = 1
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
                            // Set metadata
                            model.CreatedDate = DateTime.Now;
                            model.CreatedBy = User.Identity.Name;
                            model.DocumentType = DocumentType.Outgoing;

                            // Insert OutgoingDocument
                            var query = @"INSERT INTO Documents (
                                ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                                ReferenceNumber, ReferenceDate, ClassificationId, Status, Priority,
                                HasDeadline, DeadlineDate, Notes, PageCount, Language, IsScanned,
                                HasAttachments, IsArchived, ArchivedDate, ArchivedBy, CreatedDate, CreatedBy,
                                ModifiedDate, ModifiedBy, InstitutionId, RecipientName, RecipientPosition,
                                RecipientEmail, RecipientPhone, RecipientAddress, SentDate, SentTime,
                                DeliveryMethod, ShipmentCompany, TrackingNumber, SignedBy, SignerPosition,
                                SignedDate, HasArchiveCopy, ArchiveLocation, ShipmentStatus, NumberOfCopies,
                                IsDeliveryConfirmed, ConfirmationDate, ConfirmedBy
                            ) OUTPUT INSERTED.DocumentId VALUES (
                                @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                                @ReferenceNumber, @ReferenceDate, @ClassificationId, @Status, @Priority,
                                @HasDeadline, @DeadlineDate, @Notes, @PageCount, @Language, @IsScanned,
                                @HasAttachments, @IsArchived, @ArchivedDate, @ArchivedBy, @CreatedDate, @CreatedBy,
                                @ModifiedDate, @ModifiedBy, @InstitutionId, @RecipientName, @RecipientPosition,
                                @RecipientEmail, @RecipientPhone, @RecipientAddress, @SentDate, @SentTime,
                                @DeliveryMethod, @ShipmentCompany, @TrackingNumber, @SignedBy, @SignerPosition,
                                @SignedDate, @HasArchiveCopy, @ArchiveLocation, @ShipmentStatus, @NumberOfCopies,
                                @IsDeliveryConfirmed, @ConfirmationDate, @ConfirmedBy
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
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/outgoing/{uniqueFileName}");
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

        // GET: Manager/OutgoingDocument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            OutgoingDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 2";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);

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
                            // Update OutgoingDocument
                            var query = @"UPDATE Documents SET
                                InstitutionId = @InstitutionId,
                                RecipientName = @RecipientName,
                                RecipientPosition = @RecipientPosition,
                                RecipientEmail = @RecipientEmail,
                                RecipientPhone = @RecipientPhone,
                                RecipientAddress = @RecipientAddress,
                                Subject = @Subject,
                                Content = @Content,
                                SentDate = @SentDate,
                                SentTime = @SentTime,
                                DeliveryMethod = @DeliveryMethod,
                                ClassificationId = @ClassificationId,
                                Status = @Status,
                                Priority = @Priority,
                                HasArchiveCopy = @HasArchiveCopy,
                                ArchiveLocation = @ArchiveLocation,
                                ShipmentCompany = @ShipmentCompany,
                                TrackingNumber = @TrackingNumber,
                                SignedBy = @SignedBy,
                                SignerPosition = @SignerPosition,
                                SignedDate = @SignedDate,
                                ShipmentStatus = @ShipmentStatus,
                                Notes = @Notes,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                                WHERE DocumentId = @DocumentId AND DocumentType = 2";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", id);
                                command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
                                command.Parameters.AddWithValue("@RecipientName", model.RecipientName);
                                command.Parameters.AddWithValue("@RecipientPosition", (object)model.RecipientPosition ?? DBNull.Value);
                                command.Parameters.AddWithValue("@RecipientEmail", (object)model.RecipientEmail ?? DBNull.Value);
                                command.Parameters.AddWithValue("@RecipientPhone", (object)model.RecipientPhone ?? DBNull.Value);
                                command.Parameters.AddWithValue("@RecipientAddress", (object)model.RecipientAddress ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Subject", model.Subject);
                                command.Parameters.AddWithValue("@Content", (object)model.Content ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SentDate", (object)model.SentDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SentTime", (object)model.SentTime ?? DBNull.Value);
                                command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
                                command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
                                command.Parameters.AddWithValue("@Status", (int)model.Status);
                                command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                                command.Parameters.AddWithValue("@HasArchiveCopy", model.HasArchiveCopy);
                                command.Parameters.AddWithValue("@ArchiveLocation", (object)model.ArchiveLocation ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ShipmentCompany", (object)model.ShipmentCompany ?? DBNull.Value);
                                command.Parameters.AddWithValue("@TrackingNumber", (object)model.TrackingNumber ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SignedBy", (object)model.SignedBy ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SignerPosition", (object)model.SignerPosition ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SignedDate", (object)model.SignedDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ShipmentStatus", (int)model.ShipmentStatus);
                                command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                                command.Parameters.AddWithValue("@ModifiedBy", User.Identity.Name);

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
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/outgoing/{uniqueFileName}");
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", User.Identity?.Name ?? "System");
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
                                    attachCommand.Parameters.AddWithValue("@IsVirusScanned", false);
                                    attachCommand.Parameters.AddWithValue("@AllowDownload", true);
                                    attachCommand.Parameters.AddWithValue("@AllowPrint", true);
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
                    c.Name as ClassificationName, c.ColorCode, c.Level as ClassificationLevel,
                    u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentId = @DocumentId AND d.DocumentType = 2";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
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
                                        : reader.GetString(reader.GetOrdinal("ColorCode")),
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
                    LEFT JOIN AspNetUsers u ON dt.AssignedTo = u.Id
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
                    ShipmentStatus = 3,
                    SentDate = @SentDate,
                    SentTime = @SentTime,
                    Status = 5,
                    IsDeliveryConfirmed = 1,
                    ConfirmationDate = @ConfirmationDate,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE DocumentId = @DocumentId AND DocumentType = 2";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
                    command.Parameters.AddWithValue("@SentDate", DateTime.Now.Date);
                    command.Parameters.AddWithValue("@SentTime", new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));
                    command.Parameters.AddWithValue("@ConfirmationDate", DateTime.Now.Date);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

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
                var queryProto = "SELECT ProtocolNumber FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 2";
                using (var command = new SqlCommand(queryProto, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
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
                    var query = "DELETE FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 2";
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
                                UseSeparatorSlash, IsActive
                            ) VALUES (
                                @Year, 'H', 1, 0, 'D', 1, 0, 'B', 1, 0,
                                '{PREFIX}-{NUMBER}/{YEAR}', 4, 1, 1, 1, 1
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
            command.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
            command.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
            command.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
            command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Outgoing);
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
            command.Parameters.AddWithValue("@RecipientName", model.RecipientName);
            command.Parameters.AddWithValue("@RecipientPosition", (object)model.RecipientPosition ?? DBNull.Value);
            command.Parameters.AddWithValue("@RecipientEmail", (object)model.RecipientEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@RecipientPhone", (object)model.RecipientPhone ?? DBNull.Value);
            command.Parameters.AddWithValue("@RecipientAddress", (object)model.RecipientAddress ?? DBNull.Value);
            command.Parameters.AddWithValue("@SentDate", (object)model.SentDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@SentTime", (object)model.SentTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
            command.Parameters.AddWithValue("@ShipmentCompany", (object)model.ShipmentCompany ?? DBNull.Value);
            command.Parameters.AddWithValue("@TrackingNumber", (object)model.TrackingNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@SignedBy", (object)model.SignedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@SignerPosition", (object)model.SignerPosition ?? DBNull.Value);
            command.Parameters.AddWithValue("@SignedDate", (object)model.SignedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@HasArchiveCopy", model.HasArchiveCopy);
            command.Parameters.AddWithValue("@ArchiveLocation", (object)model.ArchiveLocation ?? DBNull.Value);
            command.Parameters.AddWithValue("@ShipmentStatus", (int)model.ShipmentStatus);
            command.Parameters.AddWithValue("@NumberOfCopies", (object)model.NumberOfCopies ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsDeliveryConfirmed", model.IsDeliveryConfirmed);
            command.Parameters.AddWithValue("@ConfirmationDate", (object)model.ConfirmationDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConfirmedBy", (object)model.ConfirmedBy ?? DBNull.Value);
        }
    }
}