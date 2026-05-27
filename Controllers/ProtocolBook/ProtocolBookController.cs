using eProtokoll.Repositories.ProtocolBook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public class ProtocolBookController : Controller
    {
        private readonly IProtocolBookRepository _protocolBookRepository;

        public ProtocolBookController(IProtocolBookRepository protocolBookRepository)
        {
            _protocolBookRepository = protocolBookRepository;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1)
        {
            var pageSize = 50;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = GetRole();

            (List<eProtokoll.Models.Document> documents, int totalItems) result;

            if (role == "Employee")
                result = await _protocolBookRepository.GetPagedForEmployeeAsync(page, pageSize, userId);
            else
                result = await _protocolBookRepository.GetPagedAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = result.totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(result.totalItems / (double)pageSize);

            ViewData["area"] = role;
            return View("~/Views/ProtocolBook/Index.cshtml", result.documents);
        }

        // ================= PRINT =================
        public async Task<IActionResult> Print()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = GetRole();

            var documents = role == "Employee"
                ? await _protocolBookRepository.GetForPrintForEmployeeAsync(userId)
                : await _protocolBookRepository.GetForPrintAsync();

            ViewData["area"] = role;
            return View("~/Views/ProtocolBook/Print.cshtml", documents);
        }

        // ================= ROLE =================
        private string GetRole()
        {
            if (User.IsInRole("Administrator")) return "Administrator";
            if (User.IsInRole("Manager")) return "Manager";
            return "Employee";
        }
    }
}