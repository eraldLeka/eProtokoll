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
    public class InternalDocumentController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public InternalDocumentController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
        }

        // GET: Manager/InternalDocument
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string department = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var documents = new List<InternalDocument>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Build dynamic query
                var queryBuilder = new StringBuilder(@"
                    SELECT d.*, 
                        c.Name as ClassificationName, c.ColorCode,
                        u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName,
                        uf.UserName as FromUserUserName, uf.FirstName as FromUserFirstName, uf.LastName as FromUserLastName,
                        ut.UserName as ToUserUserName, ut.FirstName as ToUserFirstName, ut.LastName as ToUserLastName
                    FROM Documents d
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    LEFT JOIN AspNetUsers uf ON d.FromUserId = uf.Id
                    LEFT JOIN AspNetUsers ut ON d.ToUserId = ut.Id
                    WHERE d.DocumentType = 3");

                var parameters = new List<SqlParameter>();

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.FromDepartment LIKE @SearchTerm
                        OR d.ToDepartment LIKE @SearchTerm
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

                // Department filter
                if (!string.IsNullOrEmpty(department))
                {
                    queryBuilder.Append(" AND (d.FromDepartment LIKE @Department OR d.ToDepartment LIKE @Department)");
                    parameters.Add(new SqlParameter("@Department", $"%{department}%"));
                }

                // Date range filters
                if (dateFrom.HasValue)
                {
                    queryBuilder.Append(" AND d.CreatedDate >= @DateFrom");
                    parameters.Add(new SqlParameter("@DateFrom", dateFrom.Value));
                }

                if (dateTo.HasValue)
                {
                    queryBuilder.Append(" AND d.CreatedDate <= @DateTo");
                    parameters.Add(new SqlParameter("@DateTo", dateTo.Value));
                }

                // Get total count - build separate count query
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    WHERE d.DocumentType = 3");

                // Apply same filters as main query
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.FromDepartment LIKE @SearchTerm
                        OR d.ToDepartment LIKE @SearchTerm
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

                if (!string.IsNullOrEmpty(department))
                {
                    countQueryBuilder.Append(" AND (d.FromDepartment LIKE @Department OR d.ToDepartment LIKE @Department)");
                }

                if (dateFrom.HasValue)
                {
                    countQueryBuilder.Append(" AND d.CreatedDate >= @DateFrom");
                }

                if (dateTo.HasValue)
                {
                    countQueryBuilder.Append(" AND d.CreatedDate <= @DateTo");
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
                queryBuilder.Append(@" ORDER BY d.CreatedDate DESC
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
                            var document = DocumentMapper.MapToInternalDocument(reader);

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

                            // Populate FromUser
                            if (!reader.IsDBNull(reader.GetOrdinal("FromUserUserName")))
                            {
                                document.FromUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("FromUserUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FromUserFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("FromUserLastName"))
                                };
                            }

                            // Populate ToUser
                            if (!reader.IsDBNull(reader.GetOrdinal("ToUserUserName")))
                            {
                                document.ToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("ToUserUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("ToUserFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("ToUserLastName"))
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
                ViewBag.SelectedDepartment = department;
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                // Statistics
                ViewBag.TotalInternal = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3");

                var queryToday = "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3 AND CAST(CreatedDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    ViewBag.TodayInternal = (int)await command.ExecuteScalarAsync();
                }

                var queryNeedsApproval = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = 3 AND RequiresApproval = 1 AND IsApproved = 0";
                ViewBag.NeedsApproval = await ExecuteCountQuery(connection, queryNeedsApproval);

                var queryApproved = @"SELECT COUNT(*) FROM Documents 
                    WHERE DocumentType = 3 AND IsApproved = 1";
                ViewBag.Approved = await ExecuteCountQuery(connection, queryApproved);
            }

            return View(documents);
        }

        // GET: Manager/InternalDocument/Create
        public async Task<IActionResult> Create()
        {
            var protocolNumber = await GenerateProtocolNumber();
            var now = DateTime.Now;
            var currentTime = new TimeSpan(now.Hour, now.Minute, now.Second);

            var document = new InternalDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = DateTime.Now.Date,
                ProtocolTime = currentTime,
                Status = DocumentStatus.Draft,
                Priority = Priority.Normal,
                Language = "Shqip",
                InternalType = InternalDocumentType.Memo
            };

            await LoadDropdowns();
            return View(document);
        }

        // POST: Manager/InternalDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InternalDocument model, IFormFile? attachmentFile)
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
                            model.DocumentType = DocumentType.Internal;

                            // Insert InternalDocument
                            var query = @"INSERT INTO Documents (
                                ProtocolNumber, ProtocolDate, ProtocolTime, DocumentType, Subject, Content,
                                ReferenceNumber, ReferenceDate, ClassificationId, Status, Priority,
                                HasDeadline, DeadlineDate, Notes, PageCount, Language, IsScanned,
                                HasAttachments, IsArchived, ArchivedDate, ArchivedBy, CreatedDate, CreatedBy,
                                ModifiedDate, ModifiedBy, FromUserId, FromDepartment, ToUserId, ToDepartment,
                                InternalType, RequiresApproval, RequiresResponse, ResponseDeadline
                            ) OUTPUT INSERTED.DocumentId VALUES (
                                @ProtocolNumber, @ProtocolDate, @ProtocolTime, @DocumentType, @Subject, @Content,
                                @ReferenceNumber, @ReferenceDate, @ClassificationId, @Status, @Priority,
                                @HasDeadline, @DeadlineDate, @Notes, @PageCount, @Language, @IsScanned,
                                @HasAttachments, @IsArchived, @ArchivedDate, @ArchivedBy, @CreatedDate, @CreatedBy,
                                @ModifiedDate, @ModifiedBy, @FromUserId, @FromDepartment, @ToUserId, @ToDepartment,
                                @InternalType, @RequiresApproval, @RequiresResponse, @ResponseDeadline
                            )";

                            int documentId;
                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                AddInternalDocumentParameters(command, model);
                                documentId = (int)await command.ExecuteScalarAsync();
                            }

                            // Handle file upload
                            if (attachmentFile != null && attachmentFile.Length > 0)
                            {
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "internal");
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
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/internal/{uniqueFileName}");
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
                            TempData["SuccessMessage"] = $"Dokumenti brendshëm '{model.ProtocolNumber}' u regjistrua me sukses!";
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

        // GET: Manager/InternalDocument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            InternalDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 3";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            document = DocumentMapper.MapToInternalDocument(reader);
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

            await LoadDropdowns(document.ClassificationId, document.FromUserId, document.ToUserId);
            return View(document);
        }

        // POST: Manager/InternalDocument/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InternalDocument model, IFormFile? attachmentFile)
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
                            // Update InternalDocument
                            var query = @"UPDATE Documents SET
                                FromUserId = @FromUserId,
                                FromDepartment = @FromDepartment,
                                ToUserId = @ToUserId,
                                ToDepartment = @ToDepartment,
                                InternalType = @InternalType,
                                Subject = @Subject,
                                Content = @Content,
                                ClassificationId = @ClassificationId,
                                Status = @Status,
                                Priority = @Priority,
                                RequiresApproval = @RequiresApproval,
                                RequiresResponse = @RequiresResponse,
                                ResponseDeadline = @ResponseDeadline,
                                Notes = @Notes,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                                WHERE DocumentId = @DocumentId AND DocumentType = 3";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", id);
                                command.Parameters.AddWithValue("@FromUserId", (object)model.FromUserId ?? DBNull.Value);
                                command.Parameters.AddWithValue("@FromDepartment", (object)model.FromDepartment ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ToUserId", (object)model.ToUserId ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ToDepartment", (object)model.ToDepartment ?? DBNull.Value);
                                command.Parameters.AddWithValue("@InternalType", (int)model.InternalType);
                                command.Parameters.AddWithValue("@Subject", model.Subject);
                                command.Parameters.AddWithValue("@Content", (object)model.Content ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ClassificationId", model.ClassificationId);
                                command.Parameters.AddWithValue("@Status", (int)model.Status);
                                command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                                command.Parameters.AddWithValue("@RequiresApproval", model.RequiresApproval);
                                command.Parameters.AddWithValue("@RequiresResponse", model.RequiresResponse);
                                command.Parameters.AddWithValue("@ResponseDeadline", (object)model.ResponseDeadline ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                                command.Parameters.AddWithValue("@ModifiedBy", User.Identity.Name);

                                await command.ExecuteNonQueryAsync();
                            }

                            // Handle new file upload
                            if (attachmentFile != null && attachmentFile.Length > 0)
                            {
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "internal");
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
                                    attachCommand.Parameters.AddWithValue("@FilePath", $"/uploads/internal/{uniqueFileName}");
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

            await LoadDropdowns(model.ClassificationId, model.FromUserId, model.ToUserId);
            return View(model);
        }

        // GET: Manager/InternalDocument/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            InternalDocument document = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Main document with JOINs
                var query = @"SELECT d.*, 
                    c.Name as ClassificationName, c.ColorCode,
                    u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName,
                    uf.UserName as FromUserUserName, uf.FirstName as FromUserFirstName, uf.LastName as FromUserLastName,
                    ut.UserName as ToUserUserName, ut.FirstName as ToUserFirstName, ut.LastName as ToUserLastName,
                    ua.UserName as ApproverUserName, ua.FirstName as ApproverFirstName, ua.LastName as ApproverLastName
                    FROM Documents d
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    LEFT JOIN AspNetUsers uf ON d.FromUserId = uf.Id
                    LEFT JOIN AspNetUsers ut ON d.ToUserId = ut.Id
                    LEFT JOIN AspNetUsers ua ON d.ApprovedBy = ua.Id
                    WHERE d.DocumentId = @DocumentId AND d.DocumentType = 3";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            document = DocumentMapper.MapToInternalDocument(reader);

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

                            // Populate FromUser
                            if (!reader.IsDBNull(reader.GetOrdinal("FromUserUserName")))
                            {
                                document.FromUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("FromUserUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FromUserFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("FromUserLastName"))
                                };
                            }

                            // Populate ToUser
                            if (!reader.IsDBNull(reader.GetOrdinal("ToUserUserName")))
                            {
                                document.ToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("ToUserUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("ToUserFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("ToUserLastName"))
                                };
                            }

                            // Populate Approver
                            if (!reader.IsDBNull(reader.GetOrdinal("ApproverUserName")))
                            {
                                document.Approver = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("ApproverUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("ApproverFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("ApproverLastName"))
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

        // POST: Manager/InternalDocument/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string? comment)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE Documents SET
                    IsApproved = 1,
                    ApprovedDate = @ApprovedDate,
                    ApprovedBy = @ApprovedBy,
                    ApprovalComment = @ApprovalComment,
                    Status = 5,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE DocumentId = @DocumentId AND DocumentType = 3";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentId", id);
                    command.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ApprovedBy", User.Identity?.Name ?? "System");
                    command.Parameters.AddWithValue("@ApprovalComment", (object)comment ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Dokumenti u miratua me sukses!" });
                    else
                        return Json(new { success = false, message = "Dokumenti nuk u gjet!" });
                }
            }
        }

        // POST: Manager/InternalDocument/Delete/5
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
                var queryProto = "SELECT ProtocolNumber FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 3";
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
                    var query = "DELETE FROM Documents WHERE DocumentId = @DocumentId AND DocumentType = 3";
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

        // POST: Manager/InternalDocument/DeleteAttachment
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
                                command.Parameters.AddWithValue("@ResetNumber", settings.InternalStartNumber - 1);
                                await command.ExecuteNonQueryAsync();
                            }

                            settings.Year = currentYear;
                            settings.InternalCurrentNumber = settings.InternalStartNumber - 1;
                        }

                        // Increment number
                        var updateQuery = @"UPDATE ProtocolSettings SET 
                            InternalCurrentNumber = InternalCurrentNumber + 1 
                            WHERE ProtocolSettingsId = 1";

                        using (var command = new SqlCommand(updateQuery, connection, transaction))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        settings.InternalCurrentNumber++;

                        transaction.Commit();

                        // Generate protocol number
                        var number = settings.InternalCurrentNumber.ToString(new string('0', settings.NumberPadding));

                        var protocolNumber = settings.ProtocolNumberFormat
                            .Replace("{PREFIX}", settings.InternalPrefix ?? "B")
                            .Replace("{NUMBER}", number)
                            .Replace("{YEAR}", settings.ShowYearInNumber ? currentYear.ToString() : "")
                            .Replace("{SUFFIX}", settings.InternalSuffix ?? "");

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

        private async Task LoadDropdowns(int? selectedClassificationId = null, string? selectedFromUserId = null, string? selectedToUserId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

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

                // Load Users
                var users = new List<ApplicationUser>();
                var queryUsers = "SELECT Id, UserName, FirstName, LastName FROM AspNetUsers ORDER BY FirstName, LastName";
                using (var command = new SqlCommand(queryUsers, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new ApplicationUser
                            {
                                Id = reader.GetString(reader.GetOrdinal("Id")),
                                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName"))
                            });
                        }
                    }
                }

                ViewBag.Users = new SelectList(users, "Id", "FullName");
                ViewBag.FromUsers = new SelectList(users, "Id", "FullName", selectedFromUserId);
                ViewBag.ToUsers = new SelectList(users, "Id", "FullName", selectedToUserId);
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

        private void AddInternalDocumentParameters(SqlCommand command, InternalDocument model)
        {
            command.Parameters.AddWithValue("@ProtocolNumber", model.ProtocolNumber);
            command.Parameters.AddWithValue("@ProtocolDate", model.ProtocolDate);
            command.Parameters.AddWithValue("@ProtocolTime", model.ProtocolTime);
            command.Parameters.AddWithValue("@DocumentType", (int)DocumentType.Internal);
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
            command.Parameters.AddWithValue("@FromUserId", (object)model.FromUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@FromDepartment", (object)model.FromDepartment ?? DBNull.Value);
            command.Parameters.AddWithValue("@ToUserId", (object)model.ToUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ToDepartment", (object)model.ToDepartment ?? DBNull.Value);
            command.Parameters.AddWithValue("@InternalType", (int)model.InternalType);
            command.Parameters.AddWithValue("@RequiresApproval", model.RequiresApproval);
            command.Parameters.AddWithValue("@RequiresResponse", model.RequiresResponse);
            command.Parameters.AddWithValue("@ResponseDeadline", (object)model.ResponseDeadline ?? DBNull.Value);
        }
    }
}