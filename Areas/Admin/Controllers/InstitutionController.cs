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
                institution.IsActive = true;
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

        // POST: Admin/Institution/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var institution = await _institutionRepository.GetByIdAsync(id);
                if (institution == null)
                {
                    TempData["ErrorMessage"] = "Institucioni nuk u gjet!";
                    return RedirectToAction(nameof(Index));
                }

                await _institutionRepository.DeactivateAsync(id, User.Identity?.Name ?? "System");

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u caktivizua me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë caktivizimit: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Institution/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var institution = await _institutionRepository.GetByIdAsync(id);
                if (institution == null)
                {
                    TempData["ErrorMessage"] = "Institucioni nuk u gjet!";
                    return RedirectToAction(nameof(Index));
                }

                await _institutionRepository.ActivateAsync(id, User.Identity?.Name ?? "System");

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u aktivizua me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë aktivizimit: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}