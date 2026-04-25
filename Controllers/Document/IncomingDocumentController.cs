using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eProtokoll.Models;
using eProtokoll.Repositories.Documents;
using eProtokoll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eProtokoll.Controllers.Document
{
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class IncomingDocumentController : Controller
    {
        private readonly IDocumentService _service;
        private readonly IDocumentRepository _repo;

        public IncomingDocumentController(
            IDocumentService service,
            IDocumentRepository repo)
        {
            _service = service;
            _repo = repo;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Employee";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (documents, totalItems) = await _repo.GetIncomingAsync(page, 20, userId, role);

            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.Role = role;

            return View("~/Views/IncomingDocument/Index.cshtml", documents);
        }

        public async Task<IActionResult> Create()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isEmployee = role == "Employee";

            await LoadDropdowns(isEmployee);

            var model = new IncomingDocument
            {
                Priority = Priority.Normal,
                Classification = Classification.Public,
                ReceivedDate = DateTime.Now
            };

            return View("~/Views/IncomingDocument/Create.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            IncomingDocument model,
            IFormFile? attachmentFile,
            List<int>? accessUserIds,
            string? scanSessionKey)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var userName = User.Identity?.Name ?? "Unknown";
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Employee" && model.Classification == Classification.Secret)
            {
                ModelState.AddModelError(
                    nameof(model.Classification),
                    "Employee nuk lejohet të krijojë dokumente sekrete.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(role == "Employee");
                return View("~/Views/IncomingDocument/Create.cshtml", model);
            }

            await _service.CreateIncomingAsync(
                model,
                attachmentFile,
                accessUserIds,
                scanSessionKey,
                userId,
                userName);

            TempData["SuccessMessage"] = "Dokumenti u krijua me sukses.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(bool isEmployee)
        {
            var institutions = await _repo.GetInstitutionsAsync();
            var users = await _repo.GetActiveUsersAsync();

            var classifications = Enum.GetValues<Classification>()
                .Where(c => !isEmployee
                            || c == Classification.Public
                            || c == Classification.Confidential)
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c switch
                    {
                        Classification.Public => "Publik",
                        Classification.Confidential => "I kufizuar",
                        Classification.Secret => "Sekret",
                        _ => c.ToString()
                    }
                })
                .ToList();

            ViewBag.Institutions = new SelectList(institutions, "InstitutionId", "Name");
            ViewBag.Classifications = classifications;
            ViewBag.AccessUsers = users;
        }
    }
}