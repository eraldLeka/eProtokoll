using eProtokoll.Models;
using Microsoft.AspNetCore.Http;

namespace eProtokoll.Services.Files
{
    public interface IDocumentFileService
    {
        Task<DocumentAttachment> ProcessFileAsync(
            IFormFile? uploadFile,
            string? scanSessionKey,
            int documentId,
            string originalFileNameFallback,
            string contentType,
            int userId,
            bool isSecret,
            string documentTypeFolder);
    }
}