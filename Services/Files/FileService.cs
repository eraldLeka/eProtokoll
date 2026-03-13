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
            var fullPath = Path.Combine(_uploadsFolder, storedFileName);

            if (!File.Exists(fullPath))
                File.WriteAllBytes(fullPath, fileBytes);

            return new DocumentAttachment
            {
                DocumentId = documentId,
                OriginalFileName = originalFileName,
                FileName = storedFileName,
                FilePath = storedFileName,  
                FileSize = fileBytes.Length,
                FileExtension = extension,
                ContentType = contentType,
                FileHash = hash,            
                UploadedBy = uploadedBy,
                UploadedDate = DateTime.Now
            };
        }
    }
}