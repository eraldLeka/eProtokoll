using eProtokoll.Models;

namespace eProtokoll.Services.ProtocolNumber
{
    public interface IProtocolNumberService
    {
        Task<int> GetNextDocumentNumberAsync(DocumentType type, int year);
    }
}