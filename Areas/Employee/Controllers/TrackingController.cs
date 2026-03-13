using eProtokoll.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class TrackingController : Controller
    {
        private readonly ITrackingRepository _trackingRepository;

        public TrackingController(ITrackingRepository trackingRepository)
        {
            _trackingRepository = trackingRepository;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 20;

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (trackings, totalCount) = await _trackingRepository.GetByUserAsync(page, pageSize, userId);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalItems = totalCount;

            return View(trackings);
        }

        // ================= COMPLETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            await _trackingRepository.CompleteAsync(id);

            TempData["SuccessMessage"] = "Dokumenti u shënua si i përfunduar.";
            return RedirectToAction(nameof(Index));
        }
    }
}