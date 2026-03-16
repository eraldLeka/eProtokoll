using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        public async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            var pageSize = 50;
            var documents = new List<Document>();
            var today = DateTime.Now.Date;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // WHERE clause
            var where = new StringBuilder("WHERE 1=1");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                where.Append(@"
                    AND (
                        CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) LIKE @Search
                        OR d.Subject LIKE @Search
                    )");
            }

            // Total count
            var countSql = $"SELECT COUNT(*) FROM Documents d {where}";
            int totalItems;

            using (var cmd = new SqlCommand(countSql, connection))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

                totalItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Main query
            var mainSql = $@"
                SELECT d.*,
                       u.UserName as CreatorUserName,
                       u.FirstName as CreatorFirstName,
                       u.LastName as CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {where}
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using (var cmd = new SqlCommand(mainSql, connection))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var document = DocumentMapper.MapToDocument(reader);

                    document.Classification =
                        (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

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

            // Statistics
            ViewBag.TotalDocuments = await ExecuteCountQuery(connection,
                "SELECT COUNT(*) FROM Documents");

            ViewBag.IncomingCount = await ExecuteCountQuery(connection,
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1");

            ViewBag.OutgoingCount = await ExecuteCountQuery(connection,
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2");

            ViewBag.InternalCount = await ExecuteCountQuery(connection,
                "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3");

            // Documents created today
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today",
                connection))
            {
                cmd.Parameters.AddWithValue("@Today", today);
                ViewBag.TodayDocuments = (int)await cmd.ExecuteScalarAsync();
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(documents);
        }

        public IActionResult Details(int? id, string type)
        {
            if (id == null || string.IsNullOrEmpty(type))
                return NotFound();

            return type.ToLower() switch
            {
                "incoming" => RedirectToAction("Details", "IncomingDocument", new { id }),
                "outgoing" => RedirectToAction("Details", "OutgoingDocument", new { id }),
                "internal" => RedirectToAction("Details", "InternalDocument", new { id }),
                _ => NotFound()
            };
        }

        private async Task<int> ExecuteCountQuery(SqlConnection connection, string query)
        {
            using var cmd = new SqlCommand(query, connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (int)result : 0;
        }
    }
}