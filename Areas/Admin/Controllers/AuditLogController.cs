using eProtokoll.Repositories.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class AuditLogController : Controller
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogController(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _auditLogRepository.GetAllAsync();
            return View("AuditLog", logs);
        }
    }
}