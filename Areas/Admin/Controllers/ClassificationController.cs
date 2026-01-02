using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ClassificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Classification
        public async Task<IActionResult> Index()
        {
            var classifications = await _context.Classifications
                .Include(c => c.Documents)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Level)
                .ToListAsync();

            return View(classifications);
        }

        // GET: Admin/Classification/Create
        public IActionResult Create()
        {
            var model = new Classification
            {
                IsActive = true,
                AllowPrint = true,
                AllowDownload = true,
                AllowCopy = true,
                EnableAuditLog = true,
                RetentionYears = 5,
                SortOrder = 0
            };

            return View(model);
        }

        // POST: Admin/Classification/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Classification classification)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Nëse është zgjedhur si default, heq default-in nga të tjerët
                    if (classification.IsDefault)
                    {
                        var currentDefaults = await _context.Classifications
                            .Where(c => c.IsDefault)
                            .ToListAsync();

                        foreach (var item in currentDefaults)
                        {
                            item.IsDefault = false;
                        }
                    }

                    classification.CreatedDate = DateTime.Now;
                    classification.CreatedBy = User.Identity?.Name ?? "System";

                    _context.Classifications.Add(classification);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Klasifikimi '{classification.Name}' u krijua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë ruajtjes: {ex.Message}");
                }
            }

            return View(classification);
        }

        // GET: Admin/Classification/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classification = await _context.Classifications.FindAsync(id);
            if (classification == null)
            {
                return NotFound();
            }

            return View(classification);
        }

        // POST: Admin/Classification/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Classification classification)
        {
            if (id != classification.ClassificationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Nëse është zgjedhur si default, heq default-in nga të tjerët
                    if (classification.IsDefault)
                    {
                        var currentDefaults = await _context.Classifications
                            .Where(c => c.IsDefault && c.ClassificationId != id)
                            .ToListAsync();

                        foreach (var item in currentDefaults)
                        {
                            item.IsDefault = false;
                        }
                    }

                    classification.ModifiedDate = DateTime.Now;
                    classification.ModifiedBy = User.Identity?.Name ?? "System";

                    _context.Update(classification);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Klasifikimi '{classification.Name}' u përditësua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassificationExists(classification.ClassificationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë përditësimit: {ex.Message}");
                }
            }

            return View(classification);
        }

        // GET: Admin/Classification/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classification = await _context.Classifications
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(m => m.ClassificationId == id);

            if (classification == null)
            {
                return NotFound();
            }

            return View(classification);
        }

        // POST: Admin/Classification/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var classification = await _context.Classifications
                    .Include(c => c.Documents)
                    .FirstOrDefaultAsync(c => c.ClassificationId == id);

                if (classification == null)
                {
                    TempData["ErrorMessage"] = "Klasifikimi nuk u gjet!";
                    return RedirectToAction(nameof(Index));
                }

                // Kontrollo nëse ka dokumente të lidhura
                if (classification.Documents != null && classification.Documents.Any())
                {
                    TempData["ErrorMessage"] = $"Nuk mund të fshihet! Klasifikimi '{classification.Name}' përdoret nga {classification.Documents.Count} dokumente.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Classifications.Remove(classification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Klasifikimi '{classification.Name}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Classification/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var classification = await _context.Classifications.FindAsync(id);
                if (classification == null)
                {
                    return Json(new { success = false, message = "Klasifikimi nuk u gjet!" });
                }

                classification.IsActive = !classification.IsActive;
                classification.ModifiedDate = DateTime.Now;
                classification.ModifiedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Statusi u ndryshua me sukses!",
                    isActive = classification.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Classification/BulkActivate
        [HttpPost]
        public async Task<IActionResult> BulkActivate([FromBody] List<int> ids)
        {
            try
            {
                var classifications = await _context.Classifications
                    .Where(c => ids.Contains(c.ClassificationId))
                    .ToListAsync();

                foreach (var classification in classifications)
                {
                    classification.IsActive = true;
                    classification.ModifiedDate = DateTime.Now;
                    classification.ModifiedBy = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{classifications.Count} klasifikime u aktivizuan me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Classification/BulkDeactivate
        [HttpPost]
        public async Task<IActionResult> BulkDeactivate([FromBody] List<int> ids)
        {
            try
            {
                var classifications = await _context.Classifications
                    .Where(c => ids.Contains(c.ClassificationId))
                    .ToListAsync();

                foreach (var classification in classifications)
                {
                    classification.IsActive = false;
                    classification.ModifiedDate = DateTime.Now;
                    classification.ModifiedBy = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{classifications.Count} klasifikime u çaktivizuan me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Classification/BulkDelete
        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            try
            {
                var classifications = await _context.Classifications
                    .Include(c => c.Documents)
                    .Where(c => ids.Contains(c.ClassificationId))
                    .ToListAsync();

                // Kontrollo për dokumente të lidhura
                var withDocuments = classifications.Where(c => c.Documents != null && c.Documents.Any()).ToList();
                if (withDocuments.Any())
                {
                    var names = string.Join(", ", withDocuments.Select(c => c.Name));
                    return Json(new
                    {
                        success = false,
                        message = $"Këto klasifikime nuk mund të fshihen sepse kanë dokumente të lidhura: {names}"
                    });
                }

                _context.Classifications.RemoveRange(classifications);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{classifications.Count} klasifikime u fshinë me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        private bool ClassificationExists(int id)
        {
            return _context.Classifications.Any(e => e.ClassificationId == id);
        }
    }
}