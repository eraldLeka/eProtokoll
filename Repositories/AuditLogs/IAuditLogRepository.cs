    using eProtokoll.Models;

namespace eProtokoll.Repositories.AuditLogs
{
    public interface IAuditLogRepository
    {
        Task LogAsync(AuditLog log);
        Task<List<AuditLog>> GetAllAsync();
        Task<List<AuditLog>> GetPagedAsync(int page, int pageSize);
        Task<int> CountAsync();
    }
}