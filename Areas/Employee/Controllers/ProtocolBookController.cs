using eProtokoll.Controllers.Base;
using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ProtocolBookController : BaseProtocolBookController
    {
        protected override string AreaName => "Employee";
        private readonly string _connectionString;

        public ProtocolBookController(IConfiguration configuration) : base(configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public override async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            var pageSize = 50;
            var today = DateTime.Now.Date;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder(@"
                WHERE (
                    d.Classification = 1
                    OR d.CreatedBy = @UserId
                    OR (d.Classification = 2 AND EXISTS (
                        SELECT 1 FROM DocumentPermissions dp
                        WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                    ))
                )");

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
                cmd.Parameters.AddWithValue("@UserId", userId);
                if (!string.IsNullOrEmpty(searchTerm))
                    cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");
                totalItems = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

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

            var documents = new List<Document>();

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
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
            }

            await SetStatistics(connection, today);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewData["area"] = AreaName;

            return View("~/Views/ProtocolBook/Index.cshtml", documents);
        }
    }
}