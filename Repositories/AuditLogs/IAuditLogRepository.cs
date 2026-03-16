using eProtokoll.Models;

namespace eProtokoll.Repositories.AuditLogs
{
    public interface IAuditLogRepository
    {
        Task LogAsync(AuditLog log);
        Task<List<AuditLog>> GetAllAsync();
    }
}