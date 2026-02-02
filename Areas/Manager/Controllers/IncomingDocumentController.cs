using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Security.Claims;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class IncomingDocumentController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private readonly eProtokoll.Services.IProtocolNumberService _protocolNumberService;

        public IncomingDocumentController(IConfiguration configuration, IWebHostEnvironment environment, eProtokoll.Services.IProtocolNumberService protocolNumberService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
            _protocolNumberService = protocolNumberService;
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

                // Only simple searchTerm filter is applied. Other filters are ignored.

                // Get total count
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    LEFT JOIN Institutions i ON d.InstitutionId = i.InstitutionId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE d.DocumentType = 1");

                // Apply only simple searchTerm filter to count query
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
            // Do not generate protocol number on GET. It will be generated on POST to avoid pre-reserving numbers.
            var now = DateTime.Now;
            var currentTime = new TimeSpan(now.Hour, now.Minute, now.Second);

            var document = new IncomingDocument
            {
                // ProtocolNumber will be created when the form is submitted (POST)
                ProtocolDate = DateTime.Now.Date,
                ProtocolTime = currentTime,
                ReceivedDate = DateTime.Now.Date,
                ReceivedTime = currentTime,
                Status = DocumentStatus.Registered,
                Priority = Priority.Normal,
                DeliveryMethod = DeliveryMethod.HandDelivery
            };

            await LoadDropdowns();
            return View(document);
        }

        // GET: Manager/IncomingDocument/PeekNextIncomingProtocolNumberAsync
        [HttpGet]
        public async Task<IActionResult> PeekNextIncomingProtocolNumberAsync()
        {
            try
            {
                var next = await _protocolNumberService.PeekNextIncomingProtocolNumberAsync();
                return Content(next);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        // POST: Manager/IncomingDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomingDocument model, IFormFile? attachmentFile)
        {
            // Generate protocol number on POST to avoid reserving numbers on GET
            var generatedProtocolNumber = await _protocolNumberService.GenerateNextIncomingProtocolNumberAsync();
            model.ProtocolNumber = generatedProtocolNumber;
            // Remove any modelstate entry for ProtocolNumber (it was not posted by the form)
            ModelState.Remove(nameof(model.ProtocolNumber));

            if (ModelState.IsValid)
            {
                // Set metadata
                model.CreatedDate = DateTime.Now;
                model.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier); model.DocumentType = DocumentType.Incoming;

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
    ClassificationId, Status, Priority,
    HasDeadline, DeadlineDate, Notes,HasAttachments, IsArchived, ArchivedDate, ArchivedBy, CreatedDate, CreatedBy,
    ModifiedDate, ModifiedBy, InstitutionId, SenderName,
    SenderEmail, ReceivedDate, ReceivedTime, DeliveryMethod,
    OriginalDocumentNumber, OriginalDocumentDate, RequiresResponse,
    ResponseDeadline, IsResponded, ResponseDate, Discriminator
) OUTPUT INSERTED.DocumentId VALUES (
    @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
    @ClassificationId, @Status, @Priority,
    @HasDeadline, @DeadlineDate, @Notes, 
    @HasAttachments, @IsArchived, @ArchivedDate, @ArchivedBy, @CreatedDate, @CreatedBy,
    @ModifiedDate, @ModifiedBy, @InstitutionId,@SenderName,
    @SenderEmail, @ReceivedDate, @ReceivedTime, @DeliveryMethod,
    @OriginalDocumentNumber, @OriginalDocumentDate, @RequiresResponse,
    @ResponseDeadline, @IsResponded, @ResponseDate, @Discriminator
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
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/incoming/{uniqueFileName}");
                                    attachCommand.Parameters.AddWithValue("@FileSize", attachmentFile.Length);
                                    attachCommand.Parameters.AddWithValue("@FileExtension", Path.GetExtension(attachmentFile.FileName));
                                    attachCommand.Parameters.AddWithValue("@ContentType", attachmentFile.ContentType);
                                    attachCommand.Parameters.AddWithValue("@UploadedDate", DateTime.Now);
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", User.FindFirstValue(ClaimTypes.NameIdentifier));
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
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
                            document = DocumentMapper.MapToIncomingDocument(reader);
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
                                SenderEmail = @SenderEmail,
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
                                Notes = @Notes,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                                WHERE DocumentId = @DocumentId AND DocumentType = 1";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", id);
                                command.Parameters.AddWithValue("@InstitutionId", model.InstitutionId);
                                command.Parameters.AddWithValue("@SenderName", model.SenderName);
                                command.Parameters.AddWithValue("@SenderEmail", (object)model.SenderEmail ?? DBNull.Value);
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
    ContentType, UploadedDate, UploadedBy, Category, DisplayOrder, IsPrimaryDocument
) VALUES (
    @DocumentId, @FileName, @OriginalFileName, @FilePath, @FileSize, @FileExtension,
    @ContentType, @UploadedDate, @UploadedBy, @Category, @DisplayOrder, @IsPrimaryDocument
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
                                    attachCommand.Parameters.AddWithValue("@UploadedBy", User.FindFirstValue(ClaimTypes.NameIdentifier));
                                    attachCommand.Parameters.AddWithValue("@Category", (int)FileCategory.Document);
                                    attachCommand.Parameters.AddWithValue("@DisplayOrder", 1);
                                    attachCommand.Parameters.AddWithValue("@IsPrimaryDocument", true);

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
            command.Parameters.AddWithValue("@HasDeadline", model.HasDeadline);
            command.Parameters.AddWithValue("@DeadlineDate", (object)model.DeadlineDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
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
            command.Parameters.AddWithValue("@SenderEmail", (object)model.SenderEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceivedDate", model.ReceivedDate);
            command.Parameters.AddWithValue("@ReceivedTime", model.ReceivedTime);
            command.Parameters.AddWithValue("@DeliveryMethod", (int)model.DeliveryMethod);
            command.Parameters.AddWithValue("@OriginalDocumentNumber", (object)model.OriginalDocumentNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@OriginalDocumentDate", (object)model.OriginalDocumentDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@RequiresResponse", model.RequiresResponse);
            command.Parameters.AddWithValue("@ResponseDeadline", (object)model.ResponseDeadline ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsResponded", model.IsResponded);
            command.Parameters.AddWithValue("@ResponseDate", (object)model.ResponseDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Discriminator", "IncomingDocument");

        }
    }
}