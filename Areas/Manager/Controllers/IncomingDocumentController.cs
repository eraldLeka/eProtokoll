using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Manager.Controllers
{
    [Area("Manager")]
    public class IncomingDocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public IncomingDocumentController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Manager/IncomingDocument
        public async Task<IActionResult> Index(string searchTerm = "", string status = "", string priority = "",
            string institution = "", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
        {
            var pageSize = 20;
            var query = _context.IncomingDocuments
                .Include(d => d.Institution)
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d =>
                    d.ProtocolNumber.Contains(searchTerm) ||
                    d.Subject.Contains(searchTerm) ||
                    d.SenderName.Contains(searchTerm) ||
                    d.Content.Contains(searchTerm));
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

            // Filter by institution
            if (!string.IsNullOrEmpty(institution) && int.TryParse(institution, out var institutionId))
            {
                query = query.Where(d => d.InstitutionId == institutionId);
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                query = query.Where(d => d.ReceivedDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(d => d.ReceivedDate <= dateTo.Value);
            }

            // Total count and paging
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var documents = await query
                .OrderByDescending(d => d.CreatedDate)
                .ThenByDescending(d => d.ReceivedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag for filters and dropdowns
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPriority = priority;
            ViewBag.SelectedInstitution = institution;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            ViewBag.Institutions = await _context.Institutions
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem
                {
                    Value = i.InstitutionId.ToString(),
                    Text = i.Name
                })
                .ToListAsync();

            // Statistics
            ViewBag.TotalIncoming = await _context.IncomingDocuments.CountAsync();
            ViewBag.TodayIncoming = await _context.IncomingDocuments
                .Where(d => d.ReceivedDate.Date == DateTime.Now.Date)
                .CountAsync();
            ViewBag.NeedsResponse = await _context.IncomingDocuments
                .Where(d => d.RequiresResponse && !d.IsResponded)
                .CountAsync();
            ViewBag.Overdue = await _context.IncomingDocuments
                .Where(d => d.RequiresResponse &&
                           !d.IsResponded &&
                           d.ResponseDeadline.HasValue &&
                           d.ResponseDeadline.Value < DateTime.Now)
                .CountAsync();

            return View(documents);
        }

        // GET: Manager/IncomingDocument/Create
        public async Task<IActionResult> Create()
        {
            var protocolNumber = await GenerateProtocolNumber();
            var now = DateTime.Now;
            var currentTime = new TimeSpan(now.Hour, now.Minute, now.Second);

            var document = new IncomingDocument
            {
                ProtocolNumber = protocolNumber,
                ProtocolDate = DateTime.Now.Date,
                ProtocolTime = currentTime,
                ReceivedDate = DateTime.Now.Date,
                ReceivedTime = currentTime,
                Status = DocumentStatus.Registered,
                Priority = Priority.Normal,
                Language = "Shqip",
                HasPhysicalCopy = true,
                DeliveryMethod = DeliveryMethod.HandDelivery
            };

            await LoadDropdowns();
            return View(document);
        }

        // POST: Manager/IncomingDocument/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomingDocument model, IFormFile? attachmentFile)
        {
            if (ModelState.IsValid)
            {
                // Set metadata in backend
                model.CreatedDate = DateTime.Now;
                model.CreatedBy = User.Identity.Name;
                model.DocumentType = DocumentType.Incoming;

                // Handle file upload
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "incoming");
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
                        FilePath = $"/uploads/incoming/{uniqueFileName}",
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

                _context.IncomingDocuments.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Dokumenti hyrës '{model.ProtocolNumber}' u regjistrua me sukses!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(model);
        }

        // GET: Manager/IncomingDocument/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.IncomingDocuments
                .Include(d => d.Attachments)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null) return NotFound();

            await LoadDropdowns(document.InstitutionId, document.ClassificationId);
            return View(document);
        }

        // POST: Manager/IncomingDocument/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IncomingDocument model, IFormFile? attachmentFile)
        {
            if (id != model.DocumentId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingDoc = await _context.IncomingDocuments
                    .Include(d => d.Attachments)
                    .FirstOrDefaultAsync(d => d.DocumentId == id);

                if (existingDoc == null) return NotFound();

                // Update properties
                existingDoc.InstitutionId = model.InstitutionId;
                existingDoc.SenderName = model.SenderName;
                existingDoc.SenderPosition = model.SenderPosition;
                existingDoc.SenderEmail = model.SenderEmail;
                existingDoc.SenderPhone = model.SenderPhone;
                existingDoc.Subject = model.Subject;
                existingDoc.Content = model.Content;
                existingDoc.ReceivedDate = model.ReceivedDate;
                existingDoc.ReceivedTime = model.ReceivedTime;
                existingDoc.DeliveryMethod = model.DeliveryMethod;
                existingDoc.OriginalDocumentNumber = model.OriginalDocumentNumber;
                existingDoc.OriginalDocumentDate = model.OriginalDocumentDate;
                existingDoc.ClassificationId = model.ClassificationId;
                existingDoc.Status = model.Status;
                existingDoc.Priority = model.Priority;
                existingDoc.RequiresResponse = model.RequiresResponse;
                existingDoc.ResponseDeadline = model.ResponseDeadline;
                existingDoc.HasPhysicalCopy = model.HasPhysicalCopy;
                existingDoc.PhysicalLocation = model.PhysicalLocation;
                existingDoc.Notes = model.Notes;
                existingDoc.ModifiedDate = DateTime.Now;
                existingDoc.ModifiedBy = User.Identity.Name;

                // Handle new file upload
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "incoming");
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
                        FilePath = $"/uploads/incoming/{uniqueFileName}",
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

            await LoadDropdowns(model.InstitutionId, model.ClassificationId);
            return View(model);
        }

        // GET: Manager/IncomingDocument/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.IncomingDocuments
                .Include(d => d.Institution)
                .Include(d => d.Classification)
                .Include(d => d.Creator)
                .Include(d => d.Attachments)
                .Include(d => d.Trackings)
                    .ThenInclude(t => t.AssignedToUser)
                .Include(d => d.Deadlines)
                    .ThenInclude(dl => dl.ResponsibleUser)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null) return NotFound();

            return View(document);
        }

        // POST: Manager/IncomingDocument/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.IncomingDocuments
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

                _context.IncomingDocuments.Remove(document);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Dokumenti '{document.ProtocolNumber}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Manager/IncomingDocument/DeleteAttachment
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

            settings.IncomingCurrentNumber++;
            await _context.SaveChangesAsync();

            var number = settings.IncomingCurrentNumber.ToString(new string('0', settings.NumberPadding));

            var protocolNumber = settings.ProtocolNumberFormat
                .Replace("{PREFIX}", settings.IncomingPrefix ?? "H")
                .Replace("{NUMBER}", number)
                .Replace("{YEAR}", settings.ShowYearInNumber ? currentYear.ToString() : "")
                .Replace("{SUFFIX}", settings.IncomingSuffix ?? "");

            return protocolNumber.Replace("//", "/").Replace("--", "-").Trim('-', '/');
        }

        private async Task LoadDropdowns(int? selectedInstitutionId = null, int? selectedClassificationId = null)
        {
            ViewBag.Institutions = new SelectList(
                await _context.Institutions
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.Name)
                    .ToListAsync(),
                "InstitutionId",
                "Name",
                selectedInstitutionId
            );

            ViewBag.Classifications = new SelectList(
                await _context.Classifications
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "ClassificationId",
                "Name",
                selectedClassificationId
            );
        }

        private async Task<bool> DocumentExists(int id)
        {
            return await _context.IncomingDocuments.AnyAsync(d => d.DocumentId == id);
        }
    }
}
