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
    public class TrackingController : Controller
    {
        private readonly string _connectionString;

        public TrackingController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Manager/Tracking
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string assignedTo = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var trackings = new List<DocumentTracking>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Build dynamic query
                var queryBuilder = new StringBuilder(@"
                    SELECT dt.*, 
                        d.ProtocolNumber, d.Subject, d.DocumentType,
                        uat.UserName as AssignedToUserName, uat.FirstName as AssignedToFirstName, uat.LastName as AssignedToLastName,
                        uab.UserName as AssignedByUserName, uab.FirstName as AssignedByFirstName, uab.LastName as AssignedByLastName
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN AspNetUsers uat ON dt.AssignedTo = uat.Id
                    LEFT JOIN AspNetUsers uab ON dt.AssignedBy = uab.Id
                    WHERE 1=1");

                var parameters = new List<SqlParameter>();

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryBuilder.Append(@" AND (dt.Instructions LIKE @SearchTerm 
                        OR d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm)");
                    parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                // Status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TrackingStatus>(status, out var trackStatus))
                {
                    queryBuilder.Append(" AND dt.Status = @Status");
                    parameters.Add(new SqlParameter("@Status", (int)trackStatus));
                }

                // Priority filter
                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var trackPriority))
                {
                    queryBuilder.Append(" AND dt.Priority = @Priority");
                    parameters.Add(new SqlParameter("@Priority", (int)trackPriority));
                }

                // Assigned user filter
                if (!string.IsNullOrEmpty(assignedTo))
                {
                    queryBuilder.Append(" AND dt.AssignedTo = @AssignedTo");
                    parameters.Add(new SqlParameter("@AssignedTo", assignedTo));
                }

                // Date range filters
                if (dateFrom.HasValue)
                {
                    queryBuilder.Append(" AND dt.AssignedDate >= @DateFrom");
                    parameters.Add(new SqlParameter("@DateFrom", dateFrom.Value));
                }

                if (dateTo.HasValue)
                {
                    queryBuilder.Append(" AND dt.AssignedDate <= @DateTo");
                    parameters.Add(new SqlParameter("@DateTo", dateTo.Value));
                }

                // Get total count - build separate count query
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    WHERE 1=1");

                // Apply same filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(@" AND (dt.Instructions LIKE @SearchTerm 
                        OR d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm)");
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TrackingStatus>(status, out var _))
                {
                    countQueryBuilder.Append(" AND dt.Status = @Status");
                }

                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var _))
                {
                    countQueryBuilder.Append(" AND dt.Priority = @Priority");
                }

                if (!string.IsNullOrEmpty(assignedTo))
                {
                    countQueryBuilder.Append(" AND dt.AssignedTo = @AssignedTo");
                }

                if (dateFrom.HasValue)
                {
                    countQueryBuilder.Append(" AND dt.AssignedDate >= @DateFrom");
                }

                if (dateTo.HasValue)
                {
                    countQueryBuilder.Append(" AND dt.AssignedDate <= @DateTo");
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
                queryBuilder.Append(@" ORDER BY dt.AssignedDate DESC
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
                            var tracking = TrackingMapper.MapToDocumentTracking(reader);

                            // Populate Document
                            if (!reader.IsDBNull(reader.GetOrdinal("ProtocolNumber")))
                            {
                                tracking.Document = new Document
                                {
                                    DocumentId = tracking.DocumentId,
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                    DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType"))
                                };
                            }

                            // Populate AssignedToUser
                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToUserName")))
                            {
                                tracking.AssignedToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedToUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }

                            // Populate AssignedByUser
                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByUserName")))
                            {
                                tracking.AssignedByUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedByUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }

                            trackings.Add(tracking);
                        }
                    }
                }

                // ViewBag for filters
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SelectedStatus = status;
                ViewBag.SelectedPriority = priority;
                ViewBag.SelectedAssignedTo = assignedTo;
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                // Load users for filter dropdown
                var users = new List<ApplicationUser>();
                var queryUsers = "SELECT Id, UserName, FirstName, LastName FROM AspNetUsers WHERE IsActive = 1 ORDER BY FirstName, LastName";
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

                // Statistics
                ViewBag.TotalTrackings = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM DocumentTrackings");

                var queryToday = "SELECT COUNT(*) FROM DocumentTrackings WHERE CAST(AssignedDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                    ViewBag.TodayAssigned = (int)await command.ExecuteScalarAsync();
                }

                var queryPending = @"SELECT COUNT(*) FROM DocumentTrackings 
                    WHERE Status IN (1, 2)"; // Assigned or Accepted
                ViewBag.Pending = await ExecuteCountQuery(connection, queryPending);

                var queryInProgress = @"SELECT COUNT(*) FROM DocumentTrackings WHERE Status = 3"; // InProgress
                ViewBag.InProgress = await ExecuteCountQuery(connection, queryInProgress);

                var queryCompleted = @"SELECT COUNT(*) FROM DocumentTrackings WHERE Status = 4"; // Completed
                ViewBag.Completed = await ExecuteCountQuery(connection, queryCompleted);

                var queryOverdue = @"SELECT COUNT(*) FROM DocumentTrackings 
                    WHERE DueDate < @Now AND IsCompleted = 0";
                using (var command = new SqlCommand(queryOverdue, connection))
                {
                    command.Parameters.AddWithValue("@Now", DateTime.Now);
                    ViewBag.Overdue = (int)await command.ExecuteScalarAsync();
                }
            }

            return View(trackings);
        }

        // GET: Manager/Tracking/Create
        public async Task<IActionResult> Create(int? documentId)
        {
            var tracking = new DocumentTracking
            {
                AssignedDate = DateTime.Now,
                AssignedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
                Priority = Priority.Normal,
                Status = TrackingStatus.Assigned,
                ActionType = ActionType.ForAction,
                IsActive = true
            };

            if (documentId.HasValue)
            {
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

                                tracking.DocumentId = documentId.Value;
                                ViewBag.Document = document;
                            }
                        }
                    }
                }
            }

            await LoadDropdowns();
            return View(tracking);
        }

        // POST: Manager/Tracking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentTracking model)
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
                            model.AssignedByUserId = User.Identity.Name;
                            model.IsActive = true;

                            // Calculate sequence number
                            var maxSeqQuery = "SELECT ISNULL(MAX(SequenceNumber), 0) FROM DocumentTrackings WHERE DocumentId = @DocumentId";
                            int maxSequence;
                            using (var seqCommand = new SqlCommand(maxSeqQuery, connection, transaction))
                            {
                                seqCommand.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                                maxSequence = (int)await seqCommand.ExecuteScalarAsync();
                            }
                            model.SequenceNumber = maxSequence + 1;

                            // Insert tracking
                            var query = @"INSERT INTO DocumentTrackings (
                                DocumentId, AssignedTo, AssignedBy, AssignedDate, AssignedTime, DueDate, DueTime,
                                Priority, Status, ActionType, Instructions, Notes, SequenceNumber, IsActive,
                                IsAccepted, IsInProgress, IsCompleted, IsDelegated
                            ) VALUES (
                                @DocumentId, @AssignedTo, @AssignedBy, @AssignedDate, @AssignedTime, @DueDate,
                                @Priority, @Status, @ActionType, @Instructions, @Notes, @SequenceNumber, @IsActive,
                                @IsAccepted, @IsInProgress, @IsCompleted, @IsDelegated
                            )";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                                command.Parameters.AddWithValue("@AssignedTo", model.AssignedToUserId);
                                command.Parameters.AddWithValue("@AssignedBy", model.AssignedByUserId);
                                command.Parameters.AddWithValue("@AssignedDate", model.AssignedDate);
                                command.Parameters.AddWithValue("@AssignedTime", model.AssignedTime);
                                command.Parameters.AddWithValue("@DueDate", (object)model.DueDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                                command.Parameters.AddWithValue("@Status", (int)model.Status);
                                command.Parameters.AddWithValue("@ActionType", (int)model.ActionType);
                                command.Parameters.AddWithValue("@Instructions", (object)model.Instructions ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SequenceNumber", model.SequenceNumber);
                                command.Parameters.AddWithValue("@IsActive", model.IsActive);
                                command.Parameters.AddWithValue("@IsAccepted", model.IsAccepted);
                                command.Parameters.AddWithValue("@IsInProgress", model.IsInProgress);
                                command.Parameters.AddWithValue("@IsCompleted", model.IsCompleted);
                                command.Parameters.AddWithValue("@IsDelegated", model.IsDelegated);

                                await command.ExecuteNonQueryAsync();
                            }

                            // Update document status if draft
                            var updateDocQuery = @"UPDATE Documents SET Status = 3 
                                WHERE DocumentId = @DocumentId AND Status = 1";
                            using (var updateCommand = new SqlCommand(updateDocQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                                await updateCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Dokumenti u caktua me sukses!";
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

        // GET: Manager/Tracking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            DocumentTracking tracking = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Main tracking with JOINs
                var query = @"SELECT dt.*, 
                    d.ProtocolNumber, d.Subject, d.DocumentType, d.ClassificationId,
                    c.Name as ClassificationName,
                    uat.UserName as AssignedToUserName, uat.FirstName as AssignedToFirstName, uat.LastName as AssignedToLastName,
                    uab.UserName as AssignedByUserName, uab.FirstName as AssignedByFirstName, uab.LastName as AssignedByLastName
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers uat ON dt.AssignedTo = uat.Id
                    LEFT JOIN AspNetUsers uab ON dt.AssignedBy = uab.Id
                    WHERE dt.TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            tracking = TrackingMapper.MapToDocumentTracking(reader);

                            // Populate Document
                            if (!reader.IsDBNull(reader.GetOrdinal("ProtocolNumber")))
                            {
                                tracking.Document = new Document
                                {
                                    DocumentId = tracking.DocumentId,
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                    DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                                    ClassificationId = reader.GetInt32(reader.GetOrdinal("ClassificationId"))
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

                            // Populate AssignedToUser
                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedToUserName")))
                            {
                                tracking.AssignedToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedToUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedToFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedToLastName"))
                                };
                            }

                            // Populate AssignedByUser
                            if (!reader.IsDBNull(reader.GetOrdinal("AssignedByUserName")))
                            {
                                tracking.AssignedByUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("AssignedByUserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("AssignedByFirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("AssignedByLastName"))
                                };
                            }
                        }
                    }
                }

                if (tracking == null) return NotFound();

                // Load document attachments
                var attachQuery = "SELECT * FROM DocumentAttachments WHERE DocumentId = @DocumentId ORDER BY DisplayOrder";
                tracking.Document.Attachments = new List<DocumentAttachment>();
                using (var command = new SqlCommand(attachQuery, connection))
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

                // Load delegated tracking
                if (tracking.DelegatedToTrackingId.HasValue)
                {
                    var delegQuery = @"SELECT dt.*, u.UserName, u.FirstName, u.LastName
                        FROM DocumentTrackings dt
                        LEFT JOIN AspNetUsers u ON dt.AssignedTo = u.Id
                        WHERE dt.TrackingId = @TrackingId";

                    using (var command = new SqlCommand(delegQuery, connection))
                    {
                        command.Parameters.AddWithValue("@TrackingId", tracking.DelegatedToTrackingId.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                tracking.DelegatedToTracking = TrackingMapper.MapToDocumentTracking(reader);
                                if (!reader.IsDBNull(reader.GetOrdinal("UserName")))
                                {
                                    tracking.DelegatedToTracking.AssignedToUser = new ApplicationUser
                                    {
                                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                        LastName = reader.GetString(reader.GetOrdinal("LastName"))
                                    };
                                }
                            }
                        }
                    }
                }

                // Load parent tracking
                if (tracking.ParentTrackingId.HasValue)
                {
                    var parentQuery = "SELECT * FROM DocumentTrackings WHERE TrackingId = @TrackingId";
                    using (var command = new SqlCommand(parentQuery, connection))
                    {
                        command.Parameters.AddWithValue("@TrackingId", tracking.ParentTrackingId.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                tracking.ParentTracking = TrackingMapper.MapToDocumentTracking(reader);
                            }
                        }
                    }
                }

                // Load sub-delegations
                tracking.SubDelegations = new List<DocumentTracking>();
                var subQuery = @"SELECT dt.*, u.UserName, u.FirstName, u.LastName
                    FROM DocumentTrackings dt
                    LEFT JOIN AspNetUsers u ON dt.AssignedTo = u.Id
                    WHERE dt.ParentTrackingId = @TrackingId
                    ORDER BY dt.AssignedDate DESC";

                using (var command = new SqlCommand(subQuery, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var subTracking = TrackingMapper.MapToDocumentTracking(reader);
                            if (!reader.IsDBNull(reader.GetOrdinal("UserName")))
                            {
                                subTracking.AssignedToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName"))
                                };
                            }
                            tracking.SubDelegations.Add(subTracking);
                        }
                    }
                }
            }

            return View(tracking);
        }

        // POST: Manager/Tracking/Accept/5
        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE DocumentTrackings SET
                    IsAccepted = 1,
                    AcceptedDate = @AcceptedDate,
                    Status = 2,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@AcceptedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Gjurmimi u pranua me sukses!" });
                    else
                        return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });
                }
            }
        }

        // POST: Manager/Tracking/Start/5
        [HttpPost]
        public async Task<IActionResult> Start(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE DocumentTrackings SET
                    IsInProgress = 1,
                    StartedDate = @StartedDate,
                    Status = 3,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@StartedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Gjurmimi u nis me sukses!" });
                    else
                        return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });
                }
            }
        }

        // POST: Manager/Tracking/Complete/5
        [HttpPost]
        public async Task<IActionResult> Complete(int id, string comment, int percentage)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE DocumentTrackings SET
                    IsCompleted = 1,
                    CompletedDate = @CompletedDate,
                    CompletionComment = @CompletionComment,
                    CompletionPercentage = @CompletionPercentage,
                    Status = 4,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@CompletedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@CompletionComment", (object)comment ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CompletionPercentage", percentage);
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                        return Json(new { success = true, message = "Gjurmimi u përfundua me sukses!" });
                    else
                        return Json(new { success = false, message = "Gjurmimi nuk u gjet!" });
                }
            }
        }

        // GET: Manager/Tracking/Delegate/5
        public async Task<IActionResult> Delegate(int? id)
        {
            if (id == null) return NotFound();

            DocumentTracking parentTracking = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"SELECT dt.*, 
                    d.ProtocolNumber, d.Subject,
                    u.UserName, u.FirstName, u.LastName
                    FROM DocumentTrackings dt
                    LEFT JOIN Documents d ON dt.DocumentId = d.DocumentId
                    LEFT JOIN AspNetUsers u ON dt.AssignedTo = u.Id
                    WHERE dt.TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            parentTracking = TrackingMapper.MapToDocumentTracking(reader);

                            if (!reader.IsDBNull(reader.GetOrdinal("ProtocolNumber")))
                            {
                                parentTracking.Document = new Document
                                {
                                    DocumentId = parentTracking.DocumentId,
                                    ProtocolNumber = reader.GetString(reader.GetOrdinal("ProtocolNumber")),
                                    Subject = reader.GetString(reader.GetOrdinal("Subject"))
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("UserName")))
                            {
                                parentTracking.AssignedToUser = new ApplicationUser
                                {
                                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName"))
                                };
                            }
                        }
                    }
                }
            }

            if (parentTracking == null) return NotFound();

            var newTracking = new DocumentTracking
            {
                DocumentId = parentTracking.DocumentId,
                ParentTrackingId = parentTracking.TrackingId,
                AssignedDate = DateTime.Now,
                AssignedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second),
                Priority = parentTracking.Priority,
                Status = TrackingStatus.Assigned,
                ActionType = parentTracking.ActionType,
                IsActive = true
            };

            ViewBag.ParentTracking = parentTracking;
            await LoadDropdowns();
            return View(newTracking);
        }

        // POST: Manager/Tracking/Delegate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delegate(DocumentTracking model)
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
                            model.AssignedByUserId = User.Identity.Name;
                            model.IsActive = true;

                            // Calculate sequence number
                            var maxSeqQuery = "SELECT ISNULL(MAX(SequenceNumber), 0) FROM DocumentTrackings WHERE DocumentId = @DocumentId";
                            int maxSequence;
                            using (var seqCommand = new SqlCommand(maxSeqQuery, connection, transaction))
                            {
                                seqCommand.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                                maxSequence = (int)await seqCommand.ExecuteScalarAsync();
                            }
                            model.SequenceNumber = maxSequence + 1;

                            // Insert new tracking
                            var insertQuery = @"INSERT INTO DocumentTrackings (
                                DocumentId, AssignedTo, AssignedBy, AssignedDate, AssignedTime, DueDate,
                                Priority, Status, ActionType, Instructions, Notes, SequenceNumber, IsActive,
                                ParentTrackingId, IsAccepted, IsInProgress, IsCompleted, IsDelegated
                            ) OUTPUT INSERTED.TrackingId VALUES (
                                @DocumentId, @AssignedTo, @AssignedBy, @AssignedDate, @AssignedTime, @DueDate, @DueTime,
                                @Priority, @Status, @ActionType, @Instructions, @Notes, @SequenceNumber, @IsActive,
                                @ParentTrackingId, @IsAccepted, @IsInProgress, @IsCompleted, @IsDelegated
                            )";

                            int newTrackingId;
                            using (var command = new SqlCommand(insertQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@DocumentId", model.DocumentId);
                                command.Parameters.AddWithValue("@AssignedTo", model.AssignedToUserId);
                                command.Parameters.AddWithValue("@AssignedBy", model.AssignedByUserId);
                                command.Parameters.AddWithValue("@AssignedDate", model.AssignedDate);
                                command.Parameters.AddWithValue("@AssignedTime", model.AssignedTime);
                                command.Parameters.AddWithValue("@DueDate", (object)model.DueDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Priority", (int)model.Priority);
                                command.Parameters.AddWithValue("@Status", (int)model.Status);
                                command.Parameters.AddWithValue("@ActionType", (int)model.ActionType);
                                command.Parameters.AddWithValue("@Instructions", (object)model.Instructions ?? DBNull.Value);
                                command.Parameters.AddWithValue("@Notes", (object)model.Notes ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SequenceNumber", model.SequenceNumber);
                                command.Parameters.AddWithValue("@IsActive", model.IsActive);
                                command.Parameters.AddWithValue("@ParentTrackingId", (object)model.ParentTrackingId ?? DBNull.Value);
                                command.Parameters.AddWithValue("@IsAccepted", false);
                                command.Parameters.AddWithValue("@IsInProgress", false);
                                command.Parameters.AddWithValue("@IsCompleted", false);
                                command.Parameters.AddWithValue("@IsDelegated", false);

                                newTrackingId = (int)await command.ExecuteScalarAsync();
                            }

                            // Update parent tracking
                            if (model.ParentTrackingId.HasValue)
                            {
                                var updateQuery = @"UPDATE DocumentTrackings SET
                                    IsDelegated = 1,
                                    DelegatedToTrackingId = @DelegatedToTrackingId,
                                    Status = 5,
                                    ModifiedDate = @ModifiedDate,
                                    ModifiedBy = @ModifiedBy
                                    WHERE TrackingId = @TrackingId";

                                using (var updateCommand = new SqlCommand(updateQuery, connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@TrackingId", model.ParentTrackingId.Value);
                                    updateCommand.Parameters.AddWithValue("@DelegatedToTrackingId", newTrackingId);
                                    updateCommand.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                                    updateCommand.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                                    await updateCommand.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Dokumenti u delegua me sukses!";
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

        // POST: Manager/Tracking/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"UPDATE DocumentTrackings SET
                    Status = 6,
                    IsActive = 0,
                    Notes = @Notes,
                    ModifiedDate = @ModifiedDate,
                    ModifiedBy = @ModifiedBy
                    WHERE TrackingId = @TrackingId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TrackingId", id);
                    command.Parameters.AddWithValue("@Notes", $"Anulluar: {reason}");
                    command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

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
                    var query = "DELETE FROM DocumentTrackings WHERE TrackingId = @TrackingId";
                    using (var command = new SqlCommand(query, connection))
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

        private async Task LoadDropdowns(int? selectedDocumentId = null, string? selectedUserId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Load recent documents
                var documents = new List<dynamic>();
                var queryDocs = @"SELECT TOP 100 DocumentId, ProtocolNumber, Subject 
                    FROM Documents 
                    ORDER BY CreatedDate DESC";

                using (var command = new SqlCommand(queryDocs, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new
                            {
                                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                                DisplayText = $"{reader.GetString(reader.GetOrdinal("ProtocolNumber"))} - {reader.GetString(reader.GetOrdinal("Subject"))}"
                            });
                        }
                    }
                }

                ViewBag.Documents = new SelectList(documents, "DocumentId", "DisplayText", selectedDocumentId);

                // Load active users
                var users = new List<ApplicationUser>();
                var queryUsers = @"SELECT Id, UserName, FirstName, LastName 
                    FROM AspNetUsers 
                    WHERE IsActive = 1 
                    ORDER BY FirstName, LastName";

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

                ViewBag.Users = new SelectList(users, "Id", "FullName", selectedUserId);
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