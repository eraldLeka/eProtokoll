using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text;

namespace eProtokoll.Controllers.Base
{
    public abstract class BaseProtocolBookController : Controller
    {
        private readonly string _connectionString;
        protected virtual string AreaName => "Manager";

        protected BaseProtocolBookController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public virtual async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            var pageSize = 50;
            var today = DateTime.Now.Date;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder("WHERE 1=1");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                where.Append(@"
                    AND (
                        CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) LIKE @Search
                        OR d.Subject LIKE @Search
                    )");
            }

            int totalItems;
            using (var cmd = new SqlCommand($"SELECT COUNT(*) FROM Documents d {where}", connection))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");
                totalItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var documents = await FetchDocuments(connection, where.ToString(), searchTerm, page, pageSize);

            await SetStatistics(connection, today);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewData["area"] = AreaName;

            return View("~/Views/ProtocolBook/Index.cshtml", documents);
        }

        // ── Print: Admin & Manager ────────────────────────────────────────────
        public virtual async Task<IActionResult> Print()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var documents = await FetchAllDocuments(connection);

            ViewData["area"] = AreaName;
            ViewData["InstitutionName"] = "Institucioni"; // ose nga config

            return View("~/Views/ProtocolBook/Print.cshtml", documents);
        }

        protected virtual async Task<List<Document>> FetchAllDocuments(SqlConnection connection)
        {
            var documents = new List<Document>();

            var sql = @"
                SELECT d.*,
                       u.UserName  AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName  AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                ORDER BY d.Year ASC, d.DocumentNumber ASC";

            using var cmd = new SqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var document = DocumentMapper.MapToDocument(reader);
                document.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

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

            return documents;
        }

        protected virtual async Task<List<Document>> FetchDocuments(
            SqlConnection connection,
            string where,
            string searchTerm,
            int page,
            int pageSize)
        {
            var documents = new List<Document>();

            var sql = $@"
                SELECT d.*,
                       u.UserName  AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName  AS CreatorLastName
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                {where}
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var cmd = new SqlCommand(sql, connection);

            if (!string.IsNullOrEmpty(searchTerm))
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var document = DocumentMapper.MapToDocument(reader);
                document.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

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

            return documents;
        }

        protected async Task SetStatistics(SqlConnection connection, DateTime today)
        {
            ViewBag.TotalDocuments = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents");
            ViewBag.IncomingCount = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 1");
            ViewBag.OutgoingCount = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 2");
            ViewBag.InternalCount = await ExecuteCountQuery(connection, "SELECT COUNT(*) FROM Documents WHERE DocumentType = 3");

            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Documents WHERE CAST(CreatedDate AS DATE) = @Today", connection);
            cmd.Parameters.AddWithValue("@Today", today);
            ViewBag.TodayDocuments = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public IActionResult Details(int? id, string type)
        {
            if (id == null || string.IsNullOrEmpty(type))
                return NotFound();

            var area = AreaName;

            return type.ToLower() switch
            {
                "incoming" => RedirectToAction("Details", "IncomingDocument", new { area, id }),
                "outgoing" => RedirectToAction("Details", "OutgoingDocument", new { area, id }),
                "internal" => RedirectToAction("Details", "InternalDocument", new { area, id }),
                _ => NotFound()
            };
        }

        private async Task<int> ExecuteCountQuery(SqlConnection connection, string query)
        {
            using var cmd = new SqlCommand(query, connection);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}