using eProtokoll.Models;
using eProtokoll.Repositories.Institutions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class InstitutionController : Controller
    {
        private readonly IInstitutionRepository _institutionRepository;

        public InstitutionController(IInstitutionRepository institutionRepository)
        {
            _institutionRepository = institutionRepository;
        }

        // GET: Admin/Institution
        public async Task<IActionResult> Index()
        {
            var institutions = await _institutionRepository.GetAllAsync();
            return View(institutions);
        }

        // GET: Admin/Institution/Create
        public IActionResult Create()
        {
            return View(new Institution());
        }

        // POST: Admin/Institution/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Institution institution)
        {
            if (!ModelState.IsValid)
                return View(institution);

            try
            {
                institution.CreatedDate = DateTime.Now;
                institution.CreatedBy = User.Identity?.Name ?? "System";

                await _institutionRepository.CreateAsync(institution);

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u krijua me sukses!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Gabim gjatë ruajtjes: {ex.Message}");
                return View(institution);
            }
        }

        // GET: Admin/Institution/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var institution = await _institutionRepository.GetByIdAsync(id.Value);
            if (institution == null) return NotFound();

            return View(institution);
        }

        // POST: Admin/Institution/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Institution institution)
        {
            if (id != institution.InstitutionId) return NotFound();

            if (!ModelState.IsValid)
                return View(institution);

            try
            {
                institution.ModifiedDate = DateTime.Now;
                institution.ModifiedBy = User.Identity?.Name ?? "System";

                await _institutionRepository.UpdateAsync(institution);

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u përditësua me sukses!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Gabim gjatë përditësimit: {ex.Message}");
                return View(institution);
            }
        }

        // GET: Admin/Institution/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var institution = await _institutionRepository.GetByIdAsync(id.Value);
            if (institution == null) return NotFound();

            return View(institution);
        }

        // POST: Admin/Institution/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var institution = await _institutionRepository.GetByIdAsync(id);
                if (institution == null)
                {
                    TempData["ErrorMessage"] = "Institucioni nuk u gjet!";
                    return RedirectToAction(nameof(Index));
                }

                int documentCount = await _institutionRepository.GetDocumentCountAsync(id);
                if (documentCount > 0)
                {
                    TempData["ErrorMessage"] = $"Nuk mund të fshihet! Institucioni '{institution.Name}' përdoret nga {documentCount} dokument(e).";
                    return RedirectToAction(nameof(Index));
                }

                await _institutionRepository.DeleteAsync(id);

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}