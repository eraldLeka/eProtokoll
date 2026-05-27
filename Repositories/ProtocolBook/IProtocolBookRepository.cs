using eProtokoll.Models;

namespace eProtokoll.Repositories.ProtocolBook
{
    public interface IProtocolBookRepository
    {
        Task<(List<Document> Documents, int TotalItems)> GetPagedAsync(
            int page,
            int pageSize);

        Task<(List<Document> Documents, int TotalItems)> GetPagedForEmployeeAsync(
            int page,
            int pageSize,
            int userId);

        Task<List<Document>> GetForPrintAsync();

        Task<List<Document>> GetForPrintForEmployeeAsync(int userId);
    }
}