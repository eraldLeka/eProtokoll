using eProtokoll.Models;

namespace eProtokoll.Services.Files
{
    public interface IFileStorageService
    {
        string SaveFile(byte[] fileBytes, string fileName, string folder);
        byte[] ReadFile(string filePath);
        bool Exists(string filePath);
    }
}