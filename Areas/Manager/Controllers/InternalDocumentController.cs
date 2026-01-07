using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class InternalDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public InternalDocumentController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Manager/InternalDocument
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string department = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var query = _context.InternalDocuments
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .Include(d => d.FromUser)
                .Include(d => d.ToUser)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d =>
                    d.ProtocolNumber.Contains(searchTerm) ||
                    d.Subject.Contains(searchTerm) ||
                    (d.FromDepartment != null && d.FromDepartment.Contains(searchTerm)) ||
                    (d.ToDepartment != null && d.ToDepartment.Contains(searchTerm)) ||
                    (d.Content != null && d.Content.Contains(searchTerm)));
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var docStatus))
            {
                query = query.Where(d => d.Status == docStatus);
            }

            // Filter by priority
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var docPriority))
            {
                query = query.Where(d => d.Priority == docPriority);
            }

            // Filter by department
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(d =>
                    (d.FromDepartment != null && d.FromDepartment.Contains(department)) ||
                    (d.ToDepartment != null && d.ToDepartment.Contains(department)));
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                query = query.Where(d => d.CreatedDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(d => d.CreatedDate <= dateTo.Value);
            }

            // Total count and paging
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var documents = await query
                .OrderByDescending(d => d.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag for filters
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPriority = priority;
            ViewBag.SelectedDepartment = department;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Statistics
            ViewBag.TotalInternal = await _context.InternalDocuments.CountAsync();

            var today = DateTime.Now.Date;
            ViewBag.TodayInternal = await _context.InternalDocuments
                .Where(d => d.CreatedDate.Date == today)
                .CountAsync();

            ViewBag.NeedsApproval = await _context.InternalDocuments
                .Where(d => d.RequiresApproval && !d.IsApproved)
                .CountAsync();

            ViewBag.Approved = await _context.InternalDocuments
                .Where(d => d.IsApproved)
                .CountAsync();

            return View(documents);
        }

        // GET: Manager/InternalDocument/Create
        public async Task<IActionResult> Create()
        {
            var protocolNumber = await GenerateProtocolNumber();
            var now = DateTime.Now;
            var currentTime = new TimeSpan(now.Hour, now.Minute, now.Second);

            var document = new InternalDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = DateTime.Now.Date,
                ProtocolTime = currentTime,
                Status = DocumentStatus.Draft,
                Priority = Priority.Normal,
                Language = "Shqip",
                InternalType = InternalDocumentType.Memo
            };

            await LoadDropdowns();
            return View(document);
        }

        // POST: Manager/InternalDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InternalDocument model, IFormFile? attachmentFile)
        {
            if (ModelState.IsValid)
            {
                // Set metadata
                model.CreatedDate = DateTime.Now;
                model.CreatedBy = User.Identity.Name;
                model.DocumentType = DocumentType.Internal;

                // Handle file upload
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "internal");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachmentFile.CopyToAsync(fileStream);
                    }

                    var attachment = new DocumentAttachment
                    {
                        FileName = uniqueFileName,
                        OriginalFileName = attachmentFile.FileName,
                        FilePath = $"/uploads/internal/{uniqueFileName}",
                        FileSize = attachmentFile.Length,
                        FileExtension = Path.GetExtension(attachmentFile.FileName),
                        ContentType = attachmentFile.ContentType,
                        UploadedDate = DateTime.Now,
                        UploadedBy = User.Identity.Name,
                        Category = FileCategory.Document,
                        IsVirusScanned = false,
                        AllowDownload = true,
                        AllowPrint = true,
                        DisplayOrder = 1,
                        IsPrimaryDocument = true
                    };

                    model.Attachments = new List<DocumentAttachment> { attachment };
                    model.HasAttachments = true;
                }

                _context.InternalDocuments.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Dokumenti brendshëm '{model.ProtocolNumber}' u regjistrua me sukses!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(model);
        }

        // GET: Manager/InternalDocument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.InternalDocuments
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null) return NotFound();

            await LoadDropdowns(document.ClassificationId, document.FromUserId, document.ToUserId);
            return View(document);
        }

        // POST: Manager/InternalDocument/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InternalDocument model, IFormFile? attachmentFile)
        {
            if (id != model.DocumentId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingDoc = await _context.InternalDocuments
                    .Include(d => d.Attachments)
                    .FirstOrDefaultAsync(d => d.DocumentId == id);

                if (existingDoc == null) return NotFound();

                // Update properties
                existingDoc.FromUserId = model.FromUserId;
                existingDoc.FromDepartment = model.FromDepartment;
                existingDoc.ToUserId = model.ToUserId;
                existingDoc.ToDepartment = model.ToDepartment;
                existingDoc.InternalType = model.InternalType;
                existingDoc.Subject = model.Subject;
                existingDoc.Content = model.Content;
                existingDoc.ClassificationId = model.ClassificationId;
                existingDoc.Status = model.Status;
                existingDoc.Priority = model.Priority;
                existingDoc.RequiresApproval = model.RequiresApproval;
                existingDoc.RequiresResponse = model.RequiresResponse;
                existingDoc.ResponseDeadline = model.ResponseDeadline;
                existingDoc.Notes = model.Notes;
                existingDoc.ModifiedDate = DateTime.Now;
                existingDoc.ModifiedBy = User.Identity.Name;

                // Handle new file upload
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "internal");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachmentFile.CopyToAsync(fileStream);
                    }

                    var attachment = new DocumentAttachment
                    {
                        DocumentId = existingDoc.DocumentId,
                        FileName = uniqueFileName,
                        OriginalFileName = attachmentFile.FileName,
                        FilePath = $"/uploads/internal/{uniqueFileName}",
                        FileSize = attachmentFile.Length,
                        FileExtension = Path.GetExtension(attachmentFile.FileName),
                        ContentType = attachmentFile.ContentType,
                        UploadedDate = DateTime.Now,
                        UploadedBy = User.Identity?.Name ?? "System",
                        Category = FileCategory.Document,
                        IsVirusScanned = false,
                        AllowDownload = true,
                        AllowPrint = true,
                        DisplayOrder = (existingDoc.Attachments?.Count ?? 0) + 1
                    };

                    existingDoc.Attachments ??= new List<DocumentAttachment>();
                    existingDoc.Attachments.Add(attachment);
                    existingDoc.HasAttachments = true;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Dokumenti '{existingDoc.ProtocolNumber}' u përditësua me sukses!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model.ClassificationId, model.FromUserId, model.ToUserId);
            return View(model);
        }

        // GET: Manager/InternalDocument/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.InternalDocuments
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .Include(d => d.FromUser)
                .Include(d => d.ToUser)
                .Include(d => d.Approver)
                .Include(d => d.Attachments)
                .Include(d => d.Trackings)
                    .ThenInclude(t => t.AssignedToUser)
                .Include(d => d.Deadlines)
                    .ThenInclude(dl => dl.ResponsibleUser)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null) return NotFound();

            return View(document);
        }

        // POST: Manager/InternalDocument/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string? comment)
        {
            var document = await _context.InternalDocuments.FindAsync(id);
            if (document == null)
                return Json(new { success = false, message = "Dokumenti nuk u gjet!" });

            document.IsApproved = true;
            document.ApprovedDate = DateTime.Now;
            document.ApprovedBy = User.Identity?.Name ?? "System";
            document.ApprovalComment = comment;
            document.Status = DocumentStatus.Completed;
            document.ModifiedDate = DateTime.Now;
            document.ModifiedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Dokumenti u miratua me sukses!" });
        }

        // POST: Manager/InternalDocument/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.InternalDocuments
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Dokumenti nuk u gjet!";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (document.Attachments != null)
                {
                    foreach (var attachment in document.Attachments)
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                _context.InternalDocuments.Remove(document);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Dokumenti '{document.ProtocolNumber}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Manager/InternalDocument/DeleteAttachment
        [HttpPost]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var attachment = await _context.DocumentAttachments.FindAsync(id);
            if (attachment == null)
                return Json(new { success = false, message = "Shtojca nuk u gjet!" });

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.DocumentAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Shtojca u fshi me sukses!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // ========== HELPER METHODS ==========

        private async Task<string> GenerateProtocolNumber()
        {
            var settings = await _context.ProtocolSettings.FirstOrDefaultAsync();
            var currentYear = DateTime.Now.Year;

            if (settings == null)
            {
                settings = new ProtocolSettings
                {
                    Year = currentYear,
                    IncomingPrefix = "H",
                    IncomingStartNumber = 1,
                    IncomingCurrentNumber = 0,
                    OutgoingPrefix = "D",
                    OutgoingStartNumber = 1,
                    OutgoingCurrentNumber = 0,
                    InternalPrefix = "B",
                    InternalStartNumber = 1,
                    InternalCurrentNumber = 0,
                    ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                    NumberPadding = 4,
                    AutoResetYearly = true,
                    ShowYearInNumber = true,
                    UseSeparatorSlash = true,
                    IsActive = true
                };
                _context.ProtocolSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            if (settings.AutoResetYearly && settings.Year != currentYear)
            {
                settings.Year = currentYear;
                settings.IncomingCurrentNumber = settings.IncomingStartNumber - 1;
                settings.OutgoingCurrentNumber = settings.OutgoingStartNumber - 1;
                settings.InternalCurrentNumber = settings.InternalStartNumber - 1;
            }

            settings.InternalCurrentNumber++;
            await _context.SaveChangesAsync();

            var number = settings.InternalCurrentNumber.ToString(new string('0', settings.NumberPadding));

            var protocolNumber = settings.ProtocolNumberFormat
                .Replace("{PREFIX}", settings.InternalPrefix ?? "B")
                .Replace("{NUMBER}", number)
                .Replace("{YEAR}", settings.ShowYearInNumber ? currentYear.ToString() : "")
                .Replace("{SUFFIX}", settings.InternalSuffix ?? "");

            return protocolNumber.Replace("//", "/").Replace("--", "-").Trim('-', '/');
        }

        private async Task LoadDropdowns(int? selectedClassificationId = null, string? selectedFromUserId = null, string? selectedToUserId = null)
        {
            ViewBag.Classifications = new SelectList(
                await _context.Classifications
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "ClassificationId",
                "Name",
                selectedClassificationId
            );

            // OrderBy në databazë me FirstName dhe LastName
            // Pastaj përdorim FullName për display në memorie
            var users = await _context.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "FullName");
            ViewBag.FromUsers = new SelectList(users, "Id", "FullName", selectedFromUserId);
            ViewBag.ToUsers = new SelectList(users, "Id", "FullName", selectedToUserId);
        }

        private async Task<bool> DocumentExists(int id)
        {
            return await _context.InternalDocuments.AnyAsync(d => d.DocumentId == id);
        }
    }
}