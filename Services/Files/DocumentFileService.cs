using eProtokoll.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace eProtokoll.Services.Files
{
    public class DocumentFileService : IDocumentFileService
    {
        private readonly IFileStorageService _storage;
        private readonly IFileSecurityService _security;
        private readonly IMemoryCache _cache;

        public DocumentFileService(
            IFileStorageService storage,
            IFileSecurityService security,
            IMemoryCache cache)
        {
            _storage = storage;
            _security = security;
            _cache = cache;
        }

        public async Task<DocumentAttachment> ProcessFileAsync(
            IFormFile? uploadFile,
            string? scanSessionKey,
            int documentId,
            string originalFileNameFallback,
            string contentType,
            int userId,
            bool isSecret,
            string documentTypeFolder)
        {
            byte[] fileBytes;
            string originalName;
            bool isScan = false;

            // 1. SOURCE RESOLUTION
            if (uploadFile != null && uploadFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await uploadFile.CopyToAsync(ms);

                fileBytes = ms.ToArray();
                originalName = uploadFile.FileName;
            }
            else if (!string.IsNullOrEmpty(scanSessionKey) &&
                     _cache.TryGetValue(scanSessionKey, out byte[] scanBytes))
            {
                fileBytes = scanBytes;
                originalName = originalFileNameFallback ?? "scan.pdf";
                isScan = true;

                _cache.Remove(scanSessionKey);
            }
            else
            {
                throw new Exception("No file provided");
            }

            // 2. HASH
            var hash = ComputeHash(fileBytes);
            var extension = Path.GetExtension(originalName);
            var storedFileName = $"{hash}{extension}";
            var originalSize = fileBytes.Length;

            // 3. ENCRYPT IF NEEDED
            if (isSecret)
                fileBytes = _security.Encrypt(fileBytes);

            // 4. NORMALIZE FOLDER
            documentTypeFolder = documentTypeFolder.Trim('/').Trim('\\');

            // 5. SAVE FILE (returns relative path)
            var relativePath = Path.Combine(documentTypeFolder, storedFileName)
                .Replace("\\", "/");

            _storage.SaveFile(fileBytes, storedFileName, documentTypeFolder);

            // 6. ENTITY
            return new DocumentAttachment
            {
                DocumentId = documentId,
                OriginalFileName = originalName,
                FilePath = relativePath,
                FileSize = originalSize,
                FileExtension = extension,
                FileHash = hash,
                UploadedBy = userId,
                UploadedDate = DateTime.Now,
                Category = isScan ? FileCategory.Scanned : FileCategory.PDF
            };
        }

        private string ComputeHash(byte[] data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(data)).ToLower();
        }
    }
}