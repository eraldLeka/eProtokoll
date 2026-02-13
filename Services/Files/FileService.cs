using eProtokoll.Models;
using System.Security.Cryptography;

namespace eProtokoll.Services.Files
{
    public class FileService
    {
        private readonly string _uploadsFolder;

        public FileService(string uploadsFolder)
        {
            _uploadsFolder = uploadsFolder;
            if (!Directory.Exists(_uploadsFolder))
                Directory.CreateDirectory(_uploadsFolder);
        }

        public string ComputeHash(byte[] fileBytes)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public DocumentAttachment SaveFile(
             byte[] fileBytes,
             string originalFileName,
             int documentId,
             string contentType,
              int uploadedBy)
        {
            var hash = ComputeHash(fileBytes);

            var extension = Path.GetExtension(originalFileName);
            var storedFileName = $"{hash}{extension}";

            var filePath = Path.Combine(_uploadsFolder, storedFileName);

            if (!File.Exists(filePath))
                File.WriteAllBytes(filePath, fileBytes);

            return new DocumentAttachment
            {
                DocumentId = documentId,
                OriginalFileName = originalFileName,
                FileName = storedFileName,
                FilePath = filePath,
                FileSize = fileBytes.Length,
                FileExtension = extension,
                ContentType = contentType,
                UploadedBy = uploadedBy,
                UploadedDate = DateTime.Now
            };
        }

    }
}
