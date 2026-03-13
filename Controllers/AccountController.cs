using eProtokoll.Models;
using eProtokoll.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using eProtokoll.Repositories.User;

namespace eProtokoll.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Ju lutem plotësoni të gjitha fushat.";
                return View();
            }

            // Merr përdoruesin nga databaza
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

            // Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            // ReturnUrl
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Redirect sipas rolit
            return user.Role switch
            {
                Users.UserRole.Administrator =>
                    RedirectToAction("Index", "Dashboard", new { area = "Admin" }),

                Users.UserRole.Manager =>
                    RedirectToAction("Index", "Dashboard", new { area = "Manager" }),

                _ =>
                    RedirectToAction("Index", "Dashboard", new { area = "Employee" })
            };
        }
        
        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();

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