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

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 35;

            var totalCount = await _auditLogRepository.CountAsync();
            var logs = await _auditLogRepository.GetPagedAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View("AuditLog", logs);
        }
    }
}