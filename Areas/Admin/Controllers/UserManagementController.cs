using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> Index(string searchTerm = "", string role = "", string status = "")
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u =>
                    u.FirstName.Contains(searchTerm) ||
                    u.LastName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm) ||
                    u.UserName.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(role) && Enum.TryParse<ApplicationUser.UserRole>(role, out var userRole))
            {
                query = query.Where(u => u.Role == userRole);
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status.ToLower() == "active";
                query = query.Where(u => u.IsActive == isActive);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedRole = role;
            ViewBag.SelectedStatus = status;

            return View(users);
        }

        // GET: Admin/UserManagement/Create
        public IActionResult Create()
        {
            var user = new ApplicationUser
            {
                IsActive = true
            };

            return View(user);
        }

        // POST: Admin/UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string Password, string ConfirmPassword)
        {
            bool hasErrors = false;

            // Valido password
            if (string.IsNullOrEmpty(Password))
            {
                TempData["PasswordError"] = "Fjalëkalimi është i detyrueshëm!";
                hasErrors = true;
            }
            else if (Password.Length < 6)
            {
                TempData["PasswordError"] = "Fjalëkalimi duhet të ketë të paktën 6 karaktere!";
                hasErrors = true;
            }

            if (Password != ConfirmPassword)
            {
                TempData["ConfirmPasswordError"] = "Fjalëkalimet nuk përputhen!";
                hasErrors = true;
            }

            // Valido username (duhet të jetë unik)
            if (string.IsNullOrEmpty(model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "Emri i përdoruesit është i detyrueshëm!");
                hasErrors = true;
            }
            else
            {
                var existingUsername = await _userManager.FindByNameAsync(model.UserName);
                if (existingUsername != null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Ky emër përdoruesi është i zënë!");
                    hasErrors = true;
                }
            }

            // Valido email (duhet të jetë unik)
            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email-i është i detyrueshëm!");
                hasErrors = true;
            }
            else
            {
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Ky email është i regjistruar tashmë!");
                    hasErrors = true;
                }
            }

            // Valido FirstName dhe LastName (required por jo unikë)
            if (string.IsNullOrEmpty(model.FirstName))
            {
                ModelState.AddModelError(nameof(model.FirstName), "Emri është i detyrueshëm!");
                hasErrors = true;
            }

            if (string.IsNullOrEmpty(model.LastName))
            {
                ModelState.AddModelError(nameof(model.LastName), "Mbiemri është i detyrueshëm!");
                hasErrors = true;
            }

            if (hasErrors || !ModelState.IsValid)
            {
                ViewBag.Password = Password;
                ViewBag.ConfirmPassword = ConfirmPassword;
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Position = model.Position,
                Department = model.Department,
                Role = model.Role,
                PhoneNumber = model.PhoneNumber,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Përdoruesi '{user.FullName}' u krijua me sukses!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Password = Password;
            ViewBag.ConfirmPassword = ConfirmPassword;
            return View(model);
        }

        // GET: Admin/UserManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model, string Password, string ConfirmPassword)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            bool hasErrors = false;

            // Valido password (vetëm nëse është shkruar)
            if (!string.IsNullOrEmpty(Password))
            {
                if (Password.Length < 6)
                {
                    TempData["PasswordError"] = "Fjalëkalimi duhet të ketë të paktën 6 karaktere!";
                    hasErrors = true;
                }

                if (Password != ConfirmPassword)
                {
                    TempData["ConfirmPasswordError"] = "Fjalëkalimet nuk përputhen!";
                    hasErrors = true;
                }
            }

            // Valido email nëse është ndryshuar
            if (user.Email != model.Email)
            {
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null && existingEmail.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "Ky email është i regjistruar tashmë!");
                    hasErrors = true;
                }
            }

            // Valido username nëse është ndryshuar
            if (user.UserName != model.UserName)
            {
                var existingUsername = await _userManager.FindByNameAsync(model.UserName);
                if (existingUsername != null && existingUsername.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Ky emër përdoruesi është i zënë!");
                    hasErrors = true;
                }
            }

            if (hasErrors || !ModelState.IsValid)
            {
                ViewBag.Password = Password;
                ViewBag.ConfirmPassword = ConfirmPassword;
                return View(model);
            }

            // Përditëso të dhënat
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Position = model.Position;
            user.Department = model.Department;
            user.Role = model.Role;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = model.IsActive;
            user.ModifiedData = DateTime.Now;
            user.ModifiedBy = User.Identity?.Name;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Password = Password;
                ViewBag.ConfirmPassword = ConfirmPassword;
                return View(model);
            }

            // Ndrysho password-in nëse është shkruar
            if (!string.IsNullOrEmpty(Password))
            {
                await _userManager.RemovePasswordAsync(user);
                var passwordResult = await _userManager.AddPasswordAsync(user, Password);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewBag.Password = Password;
                    ViewBag.ConfirmPassword = ConfirmPassword;
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = $"Përdoruesi '{user.FullName}' u përditësua me sukses!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Përdoruesi nuk u gjet!";
                return RedirectToAction(nameof(Index));
            }

            var hasDocuments = await _context.Documents.AnyAsync(d => d.CreatedBy == id);
            if (hasDocuments)
            {
                TempData["ErrorMessage"] = $"Nuk mund të fshihet! Përdoruesi '{user.FullName}' ka dokumente të lidhura.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Përdoruesi '{user.FullName}' u fshi me sukses!";
            }
            else
            {
                TempData["ErrorMessage"] = "Gabim gjatë fshirjes së përdoruesit!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/UserManagement/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Përdoruesi nuk u gjet!" });
            }

            user.IsActive = !user.IsActive;
            user.ModifiedData = DateTime.Now;
            user.ModifiedBy = User.Identity?.Name;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Json(new
                {
                    success = true,
                    message = "Statusi u ndryshua me sukses!",
                    isActive = user.IsActive
                });
            }

            return Json(new { success = false, message = "Gabim gjatë ndryshimit të statusit!" });
        }

        // POST: Admin/UserManagement/BulkActivate
        [HttpPost]
        public async Task<IActionResult> BulkActivate([FromBody] List<string> ids)
        {
            try
            {
                int count = 0;
                foreach (var id in ids)
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user != null)
                    {
                        user.IsActive = true;
                        user.ModifiedData = DateTime.Now;
                        user.ModifiedBy = User.Identity?.Name;
                        await _userManager.UpdateAsync(user);
                        count++;
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"{count} përdorues u aktivizuan me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/UserManagement/BulkDeactivate
        [HttpPost]
        public async Task<IActionResult> BulkDeactivate([FromBody] List<string> ids)
        {
            try
            {
                int count = 0;
                foreach (var id in ids)
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user != null)
                    {
                        user.IsActive = false;
                        user.ModifiedData = DateTime.Now;
                        user.ModifiedBy = User.Identity?.Name;
                        await _userManager.UpdateAsync(user);
                        count++;
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"{count} përdorues u çaktivizuan me sukses!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }
    }
}