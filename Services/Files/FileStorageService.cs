using Microsoft.AspNetCore.Hosting;

namespace eProtokoll.Services.Files
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseFolder;

        public FileStorageService(IWebHostEnvironment env)
        {
            _baseFolder = Path.Combine(env.WebRootPath, "uploads");

            if (!Directory.Exists(_baseFolder))
                Directory.CreateDirectory(_baseFolder);
        }

        public string SaveFile(byte[] fileBytes, string fileName, string folder)
        {
            // uploads/incoming
            var dirPath = Path.Combine(_baseFolder, folder);

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var fullPath = Path.Combine(dirPath, fileName);

            if (!File.Exists(fullPath))
            {
                File.WriteAllBytes(fullPath, fileBytes);
            }

            // ruaj RELATIVE path në DB
            return Path.Combine(folder, fileName).Replace("\\", "/");
        }

        public byte[] ReadFile(string filePath)
        {
            var fullPath = Path.Combine(_baseFolder, filePath);
            return File.ReadAllBytes(fullPath);
        }

        public bool Exists(string filePath)
        {
            var fullPath = Path.Combine(_baseFolder, filePath);
            return File.Exists(fullPath);
        }
    }
}