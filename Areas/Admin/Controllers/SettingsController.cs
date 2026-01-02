using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eProtokoll.Data;
using eProtokoll.Models;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Settings
        public async Task<IActionResult> Index()
        {
            var protocolSettings = await _context.ProtocolSettings
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.Year)
                .ToListAsync();

            // Nëse nuk ka asnjë setting, krijojmë një të re automatikisht
            if (!protocolSettings.Any())
            {
                var defaultSettings = new ProtocolSettings
                {
                    Year = DateTime.Now.Year,
                    IncomingStartNumber = 1,
                    IncomingCurrentNumber = 1,
                    IncomingPrefix = "H",
                    OutgoingStartNumber = 1,
                    OutgoingCurrentNumber = 1,
                    OutgoingPrefix = "D",
                    InternalStartNumber = 1,
                    InternalCurrentNumber = 1,
                    InternalPrefix = "B",
                    ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                    NumberPadding = 4,
                    AutoResetYearly = true,
                    ShowYearInNumber = true,
                    UseSeparatorSlash = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.ProtocolSettings.Add(defaultSettings);
                await _context.SaveChangesAsync();

                protocolSettings.Add(defaultSettings);
            }

            return View(protocolSettings);
        }

        // GET: Admin/Settings/ProtocolSettings
        public async Task<IActionResult> ProtocolSettings(int? id)
        {
            ProtocolSettings settings;

            if (id.HasValue)
            {
                settings = await _context.ProtocolSettings.FindAsync(id.Value);
                if (settings == null)
                {
                    TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                // Merr settings aktive
                settings = await _context.ProtocolSettings
                    .Where(s => s.IsActive)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Krijo settings default
                    settings = new ProtocolSettings
                    {
                        Year = DateTime.Now.Year,
                        IncomingStartNumber = 1,
                        IncomingCurrentNumber = 1,
                        IncomingPrefix = "H",
                        OutgoingStartNumber = 1,
                        OutgoingCurrentNumber = 1,
                        OutgoingPrefix = "D",
                        InternalStartNumber = 1,
                        InternalCurrentNumber = 1,
                        InternalPrefix = "B",
                        ProtocolNumberFormat = "{PREFIX}-{NUMBER}/{YEAR}",
                        NumberPadding = 4,
                        AutoResetYearly = true,
                        ShowYearInNumber = true,
                        UseSeparatorSlash = true,
                        IsActive = true
                    };
                }
            }

            return View(settings);
        }

        // POST: Admin/Settings/ProtocolSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProtocolSettings(ProtocolSettings settings)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (settings.ProtocolSettingsId == 0)
                    {
                        // Shto të re
                        // Çaktivizo të gjitha settings ekzistuese
                        var existingSettings = await _context.ProtocolSettings
                            .Where(s => s.IsActive)
                            .ToListAsync();

                        foreach (var existing in existingSettings)
                        {
                            existing.IsActive = false;
                            existing.ModifiedDate = DateTime.Now;
                            existing.ModifiedBy = User.Identity?.Name ?? "System";
                        }

                        settings.CreatedDate = DateTime.Now;
                        settings.CreatedBy = User.Identity?.Name ?? "System";
                        settings.IsActive = true;

                        _context.ProtocolSettings.Add(settings);
                    }
                    else
                    {
                        // Përditëso ekzistuesen
                        var existingSetting = await _context.ProtocolSettings.FindAsync(settings.ProtocolSettingsId);

                        if (existingSetting == null)
                        {
                            TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                            return RedirectToAction(nameof(Index));
                        }

                        // Kopjo vetëm fushat e modifikueshme
                        existingSetting.Year = settings.Year;
                        existingSetting.IncomingStartNumber = settings.IncomingStartNumber;
                        existingSetting.IncomingCurrentNumber = settings.IncomingCurrentNumber;
                        existingSetting.IncomingEndNumber = settings.IncomingEndNumber;
                        existingSetting.IncomingPrefix = settings.IncomingPrefix;
                        existingSetting.IncomingSuffix = settings.IncomingSuffix;

                        existingSetting.OutgoingStartNumber = settings.OutgoingStartNumber;
                        existingSetting.OutgoingCurrentNumber = settings.OutgoingCurrentNumber;
                        existingSetting.OutgoingEndNumber = settings.OutgoingEndNumber;
                        existingSetting.OutgoingPrefix = settings.OutgoingPrefix;
                        existingSetting.OutgoingSuffix = settings.OutgoingSuffix;

                        existingSetting.InternalStartNumber = settings.InternalStartNumber;
                        existingSetting.InternalCurrentNumber = settings.InternalCurrentNumber;
                        existingSetting.InternalEndNumber = settings.InternalEndNumber;
                        existingSetting.InternalPrefix = settings.InternalPrefix;
                        existingSetting.InternalSuffix = settings.InternalSuffix;

                        existingSetting.ProtocolNumberFormat = settings.ProtocolNumberFormat;
                        existingSetting.NumberPadding = settings.NumberPadding;
                        existingSetting.AutoResetYearly = settings.AutoResetYearly;
                        existingSetting.AllowManualEdit = settings.AllowManualEdit;
                        existingSetting.ShowYearInNumber = settings.ShowYearInNumber;
                        existingSetting.UseSeparatorSlash = settings.UseSeparatorSlash;

                        existingSetting.InstitutionName = settings.InstitutionName;
                        existingSetting.InstitutionCode = settings.InstitutionCode;
                        existingSetting.InstitutionAddress = settings.InstitutionAddress;
                        existingSetting.InstitutionPhone = settings.InstitutionPhone;
                        existingSetting.InstitutionEmail = settings.InstitutionEmail;
                        existingSetting.InstitutionWebsite = settings.InstitutionWebsite;

                        existingSetting.FiscalYearStart = settings.FiscalYearStart;
                        existingSetting.FiscalYearEnd = settings.FiscalYearEnd;
                        existingSetting.Notes = settings.Notes;

                        existingSetting.ModifiedDate = DateTime.Now;
                        existingSetting.ModifiedBy = User.Identity?.Name ?? "System";

                        _context.Update(existingSetting);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Parametrat e protokollit u ruajtën me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë ruajtjes: {ex.Message}");
                }
            }

            return View(settings);
        }

        // POST: Admin/Settings/Activate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var setting = await _context.ProtocolSettings.FindAsync(id);

                if (setting == null)
                {
                    TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                    return RedirectToAction(nameof(Index));
                }

                if (setting.IsClosed)
                {
                    TempData["ErrorMessage"] = "Nuk mund të aktivizohen parametra të mbyllur!";
                    return RedirectToAction(nameof(Index));
                }

                // Çaktivizo të gjitha
                var allSettings = await _context.ProtocolSettings.ToListAsync();
                foreach (var s in allSettings)
                {
                    s.IsActive = false;
                    s.ModifiedDate = DateTime.Now;
                    s.ModifiedBy = User.Identity?.Name ?? "System";
                }

                // Aktivizo të zgjedhurën
                setting.IsActive = true;
                setting.ModifiedDate = DateTime.Now;
                setting.ModifiedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Parametrat u aktivizuan me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Settings/Close
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            try
            {
                var setting = await _context.ProtocolSettings.FindAsync(id);

                if (setting == null)
                {
                    TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                    return RedirectToAction(nameof(Index));
                }

                if (setting.IsActive)
                {
                    TempData["ErrorMessage"] = "Nuk mund të mbyllen parametrat aktive! Aktivizo një tjetër para se të mbyllësh.";
                    return RedirectToAction(nameof(Index));
                }

                if (setting.IsClosed)
                {
                    TempData["ErrorMessage"] = "Parametrat janë të mbyllur tashmë!";
                    return RedirectToAction(nameof(Index));
                }

                setting.IsClosed = true;
                setting.ClosedDate = DateTime.Now;
                setting.ClosedBy = User.Identity?.Name ?? "System";
                setting.ModifiedDate = DateTime.Now;
                setting.ModifiedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Parametrat u mbyllën me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Settings/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var setting = await _context.ProtocolSettings.FindAsync(id);

                if (setting == null)
                {
                    TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                    return RedirectToAction(nameof(Index));
                }

                if (setting.IsActive)
                {
                    TempData["ErrorMessage"] = "Nuk mund të fshihen parametrat aktive! Aktivizo një tjetër para se të fshish.";
                    return RedirectToAction(nameof(Index));
                }

                // Kontrollo nëse ka dokumente të lidhura
                var hasDocuments = await _context.Documents
                    .AnyAsync(d => d.ProtocolDate.Year == setting.Year);

                if (hasDocuments)
                {
                    TempData["ErrorMessage"] = "Nuk mund të fshihen parametrat sepse ka dokumente të regjistruara me këto parametra!";
                    return RedirectToAction(nameof(Index));
                }

                _context.ProtocolSettings.Remove(setting);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Parametrat u fshinë me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Settings/PreviewProtocolNumber
        [HttpGet]
        public JsonResult PreviewProtocolNumber(string prefix, int number, int year,
            string format, int padding, bool showYear)
        {
            try
            {
                var previewNumber = GenerateProtocolNumber(prefix, number, year, format, padding, showYear);

                return Json(new { success = true, protocolNumber = previewNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GenerateProtocolNumber(string prefix, int number, int year,
            string format, int padding, bool showYear)
        {
            var result = format;

            result = result.Replace("{PREFIX}", prefix ?? "");
            result = result.Replace("{NUMBER}", number.ToString("D" + padding));

            if (showYear)
            {
                result = result.Replace("{YEAR}", year.ToString());
            }
            else
            {
                result = result.Replace("{YEAR}", "");
            }

            // Pastro separators të dyfishtë
            while (result.Contains("//"))
            {
                result = result.Replace("//", "/");
            }

            while (result.Contains("--"))
            {
                result = result.Replace("--", "-");
            }

            return result.Trim('/', '-');
        }
    }
}