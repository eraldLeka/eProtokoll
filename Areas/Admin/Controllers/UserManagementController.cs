using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class UserManagementController : Controller
    {
        private readonly string _connectionString;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserManagementController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> Index(string searchTerm = "", string role = "", string status = "")
        {
            var users = new List<ApplicationUser>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM AspNetUsers WHERE 1=1";
                var parameters = new List<SqlParameter>();

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query += @" AND (FirstName LIKE @SearchTerm 
                        OR LastName LIKE @SearchTerm 
                        OR Email LIKE @SearchTerm 
                        OR UserName LIKE @SearchTerm)";
                    parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                }

                // Role filter
                if (!string.IsNullOrEmpty(role) && Enum.TryParse<ApplicationUser.UserRole>(role, out var userRole))
                {
                    query += " AND Role = @Role";
                    parameters.Add(new SqlParameter("@Role", (int)userRole));
                }

                // Status filter
                if (!string.IsNullOrEmpty(status))
                {
                    bool isActive = status.ToLower() == "active";
                    query += " AND IsActive = @IsActive";
                    parameters.Add(new SqlParameter("@IsActive", isActive));
                }

                query += " ORDER BY CreatedDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(UserMapper.MapToApplicationUser(reader));
                        }
                    }
                }
            }

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

            // ===================== VALIDIM =====================
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

            if (string.IsNullOrEmpty(model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "Emri i përdoruesit është i detyrueshëm!");
                hasErrors = true;
            }
            else
            {
                var existingUsername = await FindByUsernameAsync(model.UserName);
                if (existingUsername != null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Ky emër përdoruesi është i zënë!");
                    hasErrors = true;
                }
            }

            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email-i është i detyrueshëm!");
                hasErrors = true;
            }
            else
            {
                var existingEmail = await FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Ky email është i regjistruar tashmë!");
                    hasErrors = true;
                }
            }

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

            // ===================== KRIJIMI I PËRDORUESIT =====================
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Position = model.Position,
                Department = model.Department,
                Role = model.Role,       // Enum për logjikë biznesi / UI
                PhoneNumber = model.PhoneNumber,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                // ===================== SHTO PËRDORUESIN NË IDENTITY ROLE =====================
                await _userManager.AddToRoleAsync(user, model.Role.ToString());

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

            // ===================== VALIDIMI I FJALËKALIMIT =====================
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

            // ===================== VALIDIMI I EMAIL =====================
            if (user.Email != model.Email)
            {
                var existingEmail = await FindByEmailAsync(model.Email);
                if (existingEmail != null && existingEmail.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "Ky email është i regjistruar tashmë!");
                    hasErrors = true;
                }
            }

            // ===================== VALIDIMI I USERNAME =====================
            if (user.UserName != model.UserName)
            {
                var existingUsername = await FindByUsernameAsync(model.UserName);
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

            // ===================== PËRDITËSIMI I TË DHËNAVE =====================
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Position = model.Position;
            user.Department = model.Department;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = model.IsActive;
            user.ModifiedDate = DateTime.Now;
            user.ModifiedBy = User.Identity?.Name;

            // ===================== PËRDITËSIMI I ROLE-S NË IDENTITY =====================
            if (user.Role != model.Role)
            {
                // Merr rolet aktuale të përdoruesit
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Hiq të gjitha rolet ekzistuese
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Shto rolin e ri
                await _userManager.AddToRoleAsync(user, model.Role.ToString());

                // Përditëso enum-in lokal
                user.Role = model.Role;
            }

            // ===================== PËRDITËSIMI I PËRDORUESIT =====================
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

            // ===================== NDRYSHIMI I FJALËKALIMIT =====================
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

            var hasDocuments = await CheckUserHasDocuments(id);
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
            user.ModifiedDate = DateTime.Now;
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
                        user.ModifiedDate = DateTime.Now;
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
                        user.ModifiedDate = DateTime.Now;
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

        // Helper method - Find user by username (ADO.NET)
        private async Task<ApplicationUser> FindByUsernameAsync(string username)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM AspNetUsers WHERE UserName = @UserName";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", username);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return UserMapper.MapToApplicationUser(reader);
                        }
                    }
                }
            }
            return null;
        }

        // Helper method - Find user by email (ADO.NET)
        private async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM AspNetUsers WHERE Email = @Email";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return UserMapper.MapToApplicationUser(reader);
                        }
                    }
                }
            }
            return null;
        }

        // Helper method - Check if user has documents (ADO.NET)
        private async Task<bool> CheckUserHasDocuments(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM Documents WHERE CreatedBy = @UserId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    int count = result != null ? Convert.ToInt32(result) : 0;
                    return count > 0;
                }
            }
        }
    }
}