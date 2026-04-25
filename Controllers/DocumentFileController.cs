using eProtokoll.Repositories.Documents;
using eProtokoll.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public class DocumentFileController : Controller
    {
        private readonly IDocumentRepository _repo;
        private readonly IWebHostEnvironment _env;
        private readonly IFileSecurityService _fileSecurity;

        public DocumentFileController(
            IDocumentRepository repo,
            IWebHostEnvironment env,
            IFileSecurityService fileSecurity)
        {
            _repo = repo;
            _env = env;
            _fileSecurity = fileSecurity;
        }

        [HttpGet]
        public async Task<IActionResult> View(int id, string? fileName = null)
        {
            var attachment = await _repo.GetAttachmentByIdAsync(id);
            if (attachment == null || string.IsNullOrWhiteSpace(attachment.FilePath))
                return NotFound();

            var fullPath = ResolvePhysicalPath(attachment.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = GetContentType(fullPath);

            if (IsEncryptedFile(attachment.FilePath))
            {
                var encrypted = await System.IO.File.ReadAllBytesAsync(fullPath);
                var decrypted = _fileSecurity.Decrypt(encrypted);
                return File(decrypted, contentType);
            }

            return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id, string? fileName = null)
        {
            var attachment = await _repo.GetAttachmentByIdAsync(id);
            if (attachment == null || string.IsNullOrWhiteSpace(attachment.FilePath))
                return NotFound();

            var fullPath = ResolvePhysicalPath(attachment.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = GetContentType(fullPath);

            if (IsEncryptedFile(attachment.FilePath))
            {
                var encrypted = await System.IO.File.ReadAllBytesAsync(fullPath);
                var decrypted = _fileSecurity.Decrypt(encrypted);
                return File(decrypted, contentType, attachment.OriginalFileName);
            }

            return PhysicalFile(fullPath, contentType, attachment.OriginalFileName, enableRangeProcessing: true);
        }

        private static bool IsEncryptedFile(string storedPath)
            => Path.GetFileName(storedPath).StartsWith("s_", StringComparison.OrdinalIgnoreCase);

        private string ResolvePhysicalPath(string storedPath)
        {
            var normalized = storedPath.TrimStart('/').Replace("\\", "/");
            while (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("uploads/".Length);
            }

            return Path.Combine(_env.WebRootPath, "uploads", normalized.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            return provider.TryGetContentType(path, out var contentType)
                ? contentType
                : "application/octet-stream";
        }
    }
}
