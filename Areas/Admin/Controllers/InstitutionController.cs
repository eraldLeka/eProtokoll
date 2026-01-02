using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InstitutionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstitutionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Institution
        public async Task<IActionResult> Index()
        {
            var institutions = await _context.Institutions
                .OrderBy(i => i.Name)
                .ToListAsync();

            return View(institutions);
        }

        // GET: Admin/Institution/Create
        public IActionResult Create()
        {
            var model = new Institution
            {
                IsActive = true
            };

            return View(model);
        }

        // POST: Admin/Institution/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Institution institution)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    institution.CreatedDate = DateTime.Now;
                    institution.CreatedBy = User.Identity?.Name ?? "System";

                    _context.Institutions.Add(institution);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u krijua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë ruajtjes: {ex.Message}");
                }
            }

            return View(institution);
        }

        // GET: Admin/Institution/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institution = await _context.Institutions.FindAsync(id);
            if (institution == null)
            {
                return NotFound();
            }

            return View(institution);
        }

        // POST: Admin/Institution/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Institution institution)
        {
            if (id != institution.InstitutionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    institution.ModifiedDate = DateTime.Now;
                    institution.ModifiedBy = User.Identity?.Name ?? "System";

                    _context.Update(institution);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u përditësua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstitutionExists(institution.InstitutionId))
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

            return View(institution);
        }

        // GET: Admin/Institution/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institution = await _context.Institutions
                .FirstOrDefaultAsync(m => m.InstitutionId == id);

            if (institution == null)
            {
                return NotFound();
            }

            return View(institution);
        }

        // POST: Admin/Institution/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);

                if (institution == null)
                {
                    TempData["ErrorMessage"] = "Institucioni nuk u gjet!";
                    return RedirectToAction(nameof(Index));
                }

                // Kontrollo nëse ka dokumente të lidhura (Incoming ose Outgoing)
                var hasIncomingDocs = await _context.IncomingDocuments
                    .AnyAsync(d => d.InstitutionId == id);

                var hasOutgoingDocs = await _context.OutgoingDocuments
                    .AnyAsync(d => d.InstitutionId == id);

                if (hasIncomingDocs || hasOutgoingDocs)
                {
                    TempData["ErrorMessage"] = $"Nuk mund të fshihet! Institucioni '{institution.Name}' përdoret nga dokumente.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Institutions.Remove(institution);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Institution/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var institution = await _context.Institutions.FindAsync(id);
                if (institution == null)
                {
                    return Json(new { success = false, message = "Institucioni nuk u gjet!" });
                }

                institution.IsActive = !institution.IsActive;
                institution.ModifiedDate = DateTime.Now;
                institution.ModifiedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Statusi u ndryshua me sukses!",
                    isActive = institution.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Institution/BulkActivate
        [HttpPost]
        public async Task<IActionResult> BulkActivate([FromBody] List<int> ids)
        {
            try
            {
                var institutions = await _context.Institutions
                    .Where(i => ids.Contains(i.InstitutionId))
                    .ToListAsync();

                foreach (var institution in institutions)
                {
                    institution.IsActive = true;
                    institution.ModifiedDate = DateTime.Now;
                    institution.ModifiedBy = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{institutions.Count} institucione u aktivizuan me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Institution/BulkDeactivate
        [HttpPost]
        public async Task<IActionResult> BulkDeactivate([FromBody] List<int> ids)
        {
            try
            {
                var institutions = await _context.Institutions
                    .Where(i => ids.Contains(i.InstitutionId))
                    .ToListAsync();

                foreach (var institution in institutions)
                {
                    institution.IsActive = false;
                    institution.ModifiedDate = DateTime.Now;
                    institution.ModifiedBy = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{institutions.Count} institucione u çaktivizuan me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Institution/BulkDelete
        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            try
            {
                var institutions = await _context.Institutions
                    .Where(i => ids.Contains(i.InstitutionId))
                    .ToListAsync();

                // Kontrollo për dokumente të lidhura
                var withDocuments = new List<string>();
                foreach (var institution in institutions)
                {
                    var hasIncomingDocs = await _context.IncomingDocuments
                        .AnyAsync(d => d.InstitutionId == institution.InstitutionId);

                    var hasOutgoingDocs = await _context.OutgoingDocuments
                        .AnyAsync(d => d.InstitutionId == institution.InstitutionId);

                    if (hasIncomingDocs || hasOutgoingDocs)
                    {
                        withDocuments.Add(institution.Name);
                    }
                }

                if (withDocuments.Any())
                {
                    var names = string.Join(", ", withDocuments);
                    return Json(new
                    {
                        success = false,
                        message = $"Këto institucione nuk mund të fshihen sepse kanë dokumente të lidhura: {names}"
                    });
                }

                _context.Institutions.RemoveRange(institutions);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{institutions.Count} institucione u fshinë me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        private bool InstitutionExists(int id)
        {
            return _context.Institutions.Any(e => e.InstitutionId == id);
        }
    }
}