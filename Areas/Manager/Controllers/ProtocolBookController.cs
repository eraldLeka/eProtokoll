using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class ProtocolBookController : Controller
    {
        private readonly string _connectionString;

        public ProtocolBookController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Manager/ProtocolBook
        public async Task<IActionResult> Index(string searchTerm = "", string documentType = "",
            string status = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 50;
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Build dynamic query
                var queryBuilder = new StringBuilder(@"
                    SELECT d.*, 
                        c.Name as ClassificationName, c.ColorCode,
                        u.UserName as CreatorUserName, u.FirstName as CreatorFirstName, u.LastName as CreatorLastName
                    FROM Documents d
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE 1=1");

                var parameters = new List<SqlParameter>();

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.Content LIKE @SearchTerm)");
                    parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                // Document type filter
                if (!string.IsNullOrEmpty(documentType) && Enum.TryParse<DocumentType>(documentType, out var docType))
                {
                    queryBuilder.Append(" AND d.DocumentType = @DocumentType");
                    parameters.Add(new SqlParameter("@DocumentType", (int)docType));
                }

                // Status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var docStatus))
                {
                    queryBuilder.Append(" AND d.Status = @Status");
                    parameters.Add(new SqlParameter("@Status", (int)docStatus));
                }

                // Date range filters
                if (dateFrom.HasValue)
                {
                    queryBuilder.Append(" AND d.ProtocolDate >= @DateFrom");
                    parameters.Add(new SqlParameter("@DateFrom", dateFrom.Value));
                }

                if (dateTo.HasValue)
                {
                    queryBuilder.Append(" AND d.ProtocolDate <= @DateTo");
                    parameters.Add(new SqlParameter("@DateTo", dateTo.Value));
                }

                // Get total count for pagination - build separate count query
                var countQueryBuilder = new StringBuilder(@"
                    SELECT COUNT(*) 
                    FROM Documents d
                    LEFT JOIN Classifications c ON d.ClassificationId = c.ClassificationId
                    LEFT JOIN AspNetUsers u ON d.CreatedBy = u.Id
                    WHERE 1=1");

                // Apply same filters as main query
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    countQueryBuilder.Append(@" AND (d.ProtocolNumber LIKE @SearchTerm 
                        OR d.Subject LIKE @SearchTerm 
                        OR d.Content LIKE @SearchTerm)");
                }

                if (!string.IsNullOrEmpty(documentType) && Enum.TryParse<DocumentType>(documentType, out var docTypeCount))
                {
                    countQueryBuilder.Append(" AND d.DocumentType = @DocumentType");
                }

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var docStatusCount))
                {
                    countQueryBuilder.Append(" AND d.Status = @Status");
                }

                if (dateFrom.HasValue)
                {
                    countQueryBuilder.Append(" AND d.ProtocolDate >= @DateFrom");
                }

                if (dateTo.HasValue)
                {
                    countQueryBuilder.Append(" AND d.ProtocolDate <= @DateTo");
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
                queryBuilder.Append(@" ORDER BY d.ProtocolDate DESC, d.ProtocolTime DESC
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
                            var document = DocumentMapper.MapToDocument(reader);

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
                ViewBag.SelectedDocumentType = documentType;
                ViewBag.SelectedStatus = status;
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                // Statistics - FIXED: Use Documents table with DocumentType filter
                var today = DateTime.Now.Date;

                ViewBag.TotalDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents");

                var queryToday = "SELECT COUNT(*) FROM Documents WHERE CAST(ProtocolDate AS DATE) = @Today";
                using (var command = new SqlCommand(queryToday, connection))
                {
                    command.Parameters.AddWithValue("@Today", today);
                    ViewBag.TodayDocuments = (int)await command.ExecuteScalarAsync();
                }

                // FIXED: Use Documents with DocumentType filter instead of separate tables
                ViewBag.IncomingCount = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1");
                ViewBag.OutgoingCount = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2");
                ViewBag.InternalCount = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3");
            }

            return View(documents);
        }

        // GET: Manager/ProtocolBook/Details/5
        public IActionResult Details(int? id, string type)
        {
            if (id == null || string.IsNullOrEmpty(type)) return NotFound();

            // Redirect to appropriate controller based on document type
            return type.ToLower() switch
            {
                "incoming" => RedirectToAction("Details", "IncomingDocument", new { id }),
                "outgoing" => RedirectToAction("Details", "OutgoingDocument", new { id }),
                "internal" => RedirectToAction("Details", "InternalDocument", new { id }),
                _ => NotFound()
            };
        }

        // Helper method për COUNT queries 
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