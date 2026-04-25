using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public class ProtocolBookController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ProtocolBookController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1)
        {
            var pageSize = 50;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = GetRole();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder("WHERE 1=1");

            // ================= ROLE FILTER =================
            if (role == "Employee")
            {
                where.Clear();
                where.Append(@"
                    WHERE (
                        d.Classification = 1
                        OR d.CreatedBy = @UserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                        ))
                    )");
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                where.Append(@"
                    AND (
                        CAST(d.DocumentNumber AS VARCHAR) + '/' + CAST(d.Year AS VARCHAR) LIKE @Search
                        OR d.Subject LIKE @Search
                    )");
            }

            var sql = $@"
                SELECT d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName,
                       la.AttachmentId AS LatestAttachmentId,
                       la.OriginalFileName AS LatestAttachmentName,
                       la.FilePath AS LatestAttachmentPath,
                       la.FileExtension AS LatestAttachmentExtension,
                       la.FileSize AS LatestAttachmentSize,
                       la.UploadedDate AS LatestAttachmentUploadedDate,
                       la.UploadedBy AS LatestAttachmentUploadedBy,
                       la.Category AS LatestAttachmentCategory
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                OUTER APPLY (
                    SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                 a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                    FROM DocumentAttachments a
                    WHERE a.DocumentId = d.DocumentId
                    ORDER BY a.UploadedDate DESC
                ) la
                {where}
                ORDER BY d.Year DESC, d.DocumentNumber DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var documents = new List<eProtokoll.Models.Document>();

            using var cmd = new SqlCommand(sql, connection);

            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            if (role == "Employee")
                cmd.Parameters.AddWithValue("@UserId", userId);

            if (!string.IsNullOrEmpty(searchTerm))
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var doc = DocumentMapper.MapToDocument(reader);
                doc.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));

                documents.Add(doc);
            }

            ViewData["area"] = role;
            return View("~/Views/ProtocolBook/Index.cshtml", documents);
        }

        // ================= PRINT =================
        public async Task<IActionResult> Print()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = GetRole();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var where = new StringBuilder("WHERE 1=1");

            if (role == "Employee")
            {
                where.Clear();
                where.Append(@"
                    WHERE (
                        d.Classification = 1
                        OR d.CreatedBy = @UserId
                        OR (d.Classification = 2 AND EXISTS (
                            SELECT 1 FROM DocumentPermissions dp
                            WHERE dp.DocumentId = d.DocumentId AND dp.UserId = @UserId
                        ))
                    )");
            }

            var sql = $@"
                SELECT d.*,
                       u.UserName AS CreatorUserName,
                       u.FirstName AS CreatorFirstName,
                       u.LastName AS CreatorLastName,
                       la.AttachmentId AS LatestAttachmentId,
                       la.OriginalFileName AS LatestAttachmentName,
                       la.FilePath AS LatestAttachmentPath,
                       la.FileExtension AS LatestAttachmentExtension,
                       la.FileSize AS LatestAttachmentSize,
                       la.UploadedDate AS LatestAttachmentUploadedDate,
                       la.UploadedBy AS LatestAttachmentUploadedBy,
                       la.Category AS LatestAttachmentCategory
                FROM Documents d
                LEFT JOIN Users u ON d.CreatedBy = u.Id
                OUTER APPLY (
                    SELECT TOP 1 a.AttachmentId, a.OriginalFileName, a.FilePath, a.FileExtension,
                                 a.FileSize, a.UploadedDate, a.UploadedBy, a.Category
                    FROM DocumentAttachments a
                    WHERE a.DocumentId = d.DocumentId
                    ORDER BY a.UploadedDate DESC
                ) la
                {where}
                ORDER BY d.Year ASC, d.DocumentNumber ASC";

            var documents = new List<eProtokoll.Models.Document>();

            using var cmd = new SqlCommand(sql, connection);

            if (role == "Employee")
                cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var doc = DocumentMapper.MapToDocument(reader);
                doc.Classification = (Classification)reader.GetInt32(reader.GetOrdinal("Classification"));
                documents.Add(      doc);
            }

            ViewData["area"] = role;
            return View("~/Views/ProtocolBook/Print.cshtml", documents);
        }

        // ================= ROLE =================
        private string GetRole()
        {
            if (User.IsInRole("Administrator")) return "Administrator";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }
    }
}