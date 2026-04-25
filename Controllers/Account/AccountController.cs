using eProtokoll.Models;
using eProtokoll.Helpers;
using eProtokoll.Repositories.AuditLogs;
using eProtokoll.Repositories.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eProtokoll.Controllers.Account
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public AccountController(
            IUserRepository userRepository,
            IAuditLogRepository auditLogRepository)
        {
            _userRepository = userRepository;
            _auditLogRepository = auditLogRepository;
        }

        // GET: Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username,
            string password,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Ju lutem plotësoni të gjitha fushat.";
                return View();
            }

            var user = await _userRepository.GetByUsernameAsync(username);

            if (user == null || !PasswordHelper.Verify(password, user.PasswordHash))
            {
                ViewBag.ErrorMessage = "Username ose password i gabuar.";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.ErrorMessage = "Llogaria është çaktivizuar.";
                return View();
            }

            // Role mapping (stable)
            string role = user.Role switch
            {
                Users.UserRole.Administrator => "Administrator",
                Users.UserRole.Manager => "Manager",
                Users.UserRole.Employee => "Employee",
                _ => "Employee"
            };

            // Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            // Audit log
            await _auditLogRepository.LogAsync(new AuditLog
            {
                UserId = user.Id,
                UserName = user.UserName,
                Action = "Login",
                Description = $"Hyrje në sistem nga roli {role}",
                Timestamp = DateTime.Now
            });

            // Safe returnUrl redirect
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // 🔥 FIX KRYESOR: Dashboard GLOBAL (pa area)
            return RedirectToAction("Index", "Dashboard");
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var userName = User.Identity?.Name ?? "Unknown";

            if (userId > 0)
            {
                await _auditLogRepository.LogAsync(new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = "Logout",
                    Description = "Dalje nga sistemi",
                    Timestamp = DateTime.Now
                });
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        // Access Denied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}