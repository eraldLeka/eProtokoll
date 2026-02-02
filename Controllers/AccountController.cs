using eProtokoll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace eProtokoll.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        //login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username,
            string password,
            bool rememberMe,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Ju lutem plotësoni të gjitha fushat.";
                return View();
            }

            // Login standard Identity
            var result = await _signInManager.PasswordSignInAsync(
                username,
                password,
                rememberMe,
                lockoutOnFailure: false
            );

            if (!result.Succeeded)
            {
                ViewBag.ErrorMessage = "Username ose password i gabuar.";
                return View();
            }

            // if user account is not active
            var user = await _userManager.FindByNameAsync(username);
            if (user != null && !user.IsActive)
            {
                await _signInManager.SignOutAsync();
                ViewBag.ErrorMessage = "Llogaria është çaktivizuar.";
                return View();
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Redirect according to Identity Role
            if (await _userManager.IsInRoleAsync(user!, "Administrator"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            if (await _userManager.IsInRoleAsync(user!, "Manager"))
                return RedirectToAction("Index", "Dashboard", new { area = "Manager" });

            return RedirectToAction("Index", "Dashboard", new { area = "Employee" });
        }

        //logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        //access denied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
