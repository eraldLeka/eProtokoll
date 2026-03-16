using eProtokoll.Helpers;
using eProtokoll.Models;
using eProtokoll.Repositories.User;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class UserManagementController : Controller
    {
        private readonly string _connectionString;
        private readonly IUserRepository _userRepository;

        public UserManagementController(
            IConfiguration configuration,
            IUserRepository userRepository)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _userRepository = userRepository;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAllAsync();
            return View(users);
        }
        // GET: Admin/UserManagement/Create
        public IActionResult Create()
        {
            return View(new Users { IsActive = true });
        }

        // POST: Admin/UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Users model, string Password, string ConfirmPassword)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ModifedDate");
            ModelState.Remove("FullName");
            bool hasErrors = false;

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

            var user = new Users
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
                PasswordHash = PasswordHelper.Hash(Password)
            };

            await _userRepository.CreateAsync(user);

            TempData["SuccessMessage"] = $"Përdoruesi '{user.FullName}' u krijua me sukses!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/UserManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Admin/UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Users model, string Password, string ConfirmPassword)
        {
            if (id != model.Id) return NotFound();

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            bool hasErrors = false;

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

            if (user.Email != model.Email)
            {
                var existingEmail = await FindByEmailAsync(model.Email);
                if (existingEmail != null && existingEmail.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "Ky email është i regjistruar tashmë!");
                    hasErrors = true;
                }
            }

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

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Position = model.Position;
            user.Department = model.Department;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = model.IsActive;
            user.Role = model.Role;
            user.ModifiedDate = DateTime.Now;
            user.ModifiedBy = User.Identity?.Name;

            if (!string.IsNullOrEmpty(Password))
                user.PasswordHash = PasswordHelper.Hash(Password);

            await _userRepository.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Përdoruesi '{user.FullName}' u përditësua me sukses!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/UserManagement/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Përdoruesi nuk u gjet!";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            user.ModifiedDate = DateTime.Now;
            user.ModifiedBy = User.Identity?.Name;

            await _userRepository.UpdateAsync(user);

            var statusText = user.IsActive ? "aktivizuar" : "çaktivizuar";
            TempData["SuccessMessage"] = $"Përdoruesi '{user.FullName}' u {statusText} me sukses!";
            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private async Task<Users?> FindByUsernameAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Users WHERE UserName = @UserName", connection);
            command.Parameters.AddWithValue("@UserName", username);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return UserMapper.Map(reader);
            return null;
        }

        private async Task<Users?> FindByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return UserMapper.Map(reader);
            return null;
        }

        private async Task<bool> CheckUserHasDocuments(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Documents WHERE CreatedBy = @UserId", connection);
            command.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
    }
}