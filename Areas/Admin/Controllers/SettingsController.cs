using eProtokoll.Models;
using eProtokoll.Services.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]

    public class SettingsController : Controller
    {
        private readonly string _connectionString;
        private readonly eProtokoll.Services.IProtocolNumberService _protocolNumberService;

        public SettingsController(IConfiguration configuration, eProtokoll.Services.IProtocolNumberService protocolNumberService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _protocolNumberService = protocolNumberService;
        }

        // GET: Admin/Settings
        public async Task<IActionResult> Index()
        {
            var protocolSettings = new List<ProtocolSettings>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM ProtocolSettings ORDER BY IsActive DESC, Year DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            protocolSettings.Add(ProtocolSettingsMapper.MapToProtocolSettings(reader));
                        }
                    }
                }

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

                    var queryInsert = @"INSERT INTO ProtocolSettings 
                        (Year, IncomingStartNumber, IncomingCurrentNumber, IncomingPrefix,
                        OutgoingStartNumber, OutgoingCurrentNumber, OutgoingPrefix,
                        InternalStartNumber, InternalCurrentNumber, InternalPrefix,
                        ProtocolNumberFormat, NumberPadding, AutoResetYearly, ShowYearInNumber,
                        UseSeparatorSlash, IsActive, CreatedDate, CreatedBy)
                        VALUES 
                        (@Year, @IncomingStartNumber, @IncomingCurrentNumber, @IncomingPrefix,
                        @OutgoingStartNumber, @OutgoingCurrentNumber, @OutgoingPrefix,
                        @InternalStartNumber, @InternalCurrentNumber, @InternalPrefix,
                        @ProtocolNumberFormat, @NumberPadding, @AutoResetYearly, @ShowYearInNumber,
                        @UseSeparatorSlash, @IsActive, @CreatedDate, @CreatedBy)";

                    using (var command = new SqlCommand(queryInsert, connection))
                    {
                        command.Parameters.AddWithValue("@Year", defaultSettings.Year);
                        command.Parameters.AddWithValue("@IncomingStartNumber", defaultSettings.IncomingStartNumber);
                        command.Parameters.AddWithValue("@IncomingCurrentNumber", defaultSettings.IncomingCurrentNumber);
                        command.Parameters.AddWithValue("@IncomingPrefix", (object)defaultSettings.IncomingPrefix ?? DBNull.Value);
                        command.Parameters.AddWithValue("@OutgoingStartNumber", defaultSettings.OutgoingStartNumber);
                        command.Parameters.AddWithValue("@OutgoingCurrentNumber", defaultSettings.OutgoingCurrentNumber);
                        command.Parameters.AddWithValue("@OutgoingPrefix", (object)defaultSettings.OutgoingPrefix ?? DBNull.Value);
                        command.Parameters.AddWithValue("@InternalStartNumber", defaultSettings.InternalStartNumber);
                        command.Parameters.AddWithValue("@InternalCurrentNumber", defaultSettings.InternalCurrentNumber);
                        command.Parameters.AddWithValue("@InternalPrefix", (object)defaultSettings.InternalPrefix ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ProtocolNumberFormat", (object)defaultSettings.ProtocolNumberFormat ?? DBNull.Value);
                        command.Parameters.AddWithValue("@NumberPadding", defaultSettings.NumberPadding);
                        command.Parameters.AddWithValue("@AutoResetYearly", defaultSettings.AutoResetYearly);
                        command.Parameters.AddWithValue("@ShowYearInNumber", defaultSettings.ShowYearInNumber);
                        command.Parameters.AddWithValue("@UseSeparatorSlash", defaultSettings.UseSeparatorSlash);
                        command.Parameters.AddWithValue("@IsActive", defaultSettings.IsActive);
                        command.Parameters.AddWithValue("@CreatedDate", defaultSettings.CreatedDate);
                        command.Parameters.AddWithValue("@CreatedBy", (object)defaultSettings.CreatedBy ?? DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }

                    protocolSettings.Add(defaultSettings);
                }
            }

            return View(protocolSettings);
        }

        // GET: Admin/Settings/ProtocolSettings
        public async Task<IActionResult> ProtocolSettings(int? id)
        {
            ProtocolSettings settings = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                if (id.HasValue)
                {
                    var query = "SELECT * FROM ProtocolSettings WHERE ProtocolSettingsId = @Id";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);
                            }
                        }
                    }

                    if (settings == null)
                    {
                        TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    // Merr settings aktive
                    var query = "SELECT * FROM ProtocolSettings WHERE IsActive = 1";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                settings = ProtocolSettingsMapper.MapToProtocolSettings(reader);
                            }
                        }
                    }

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
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        if (settings.ProtocolSettingsId == 0)
                        {
                            // Shto të re - Çaktivizo të gjitha ekzistuese
                            var queryDeactivate = "UPDATE ProtocolSettings SET IsActive = 0, ModifiedDate = @ModifiedDate, ModifiedBy = @ModifiedBy WHERE IsActive = 1";
                            using (var command = new SqlCommand(queryDeactivate, connection))
                            {
                                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                                command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                                await command.ExecuteNonQueryAsync();
                            }

                            settings.CreatedDate = DateTime.Now;
                            settings.CreatedBy = User.Identity?.Name ?? "System";
                            settings.IsActive = true;

                            var queryInsert = @"INSERT INTO ProtocolSettings 
                                (Year, IncomingStartNumber, IncomingCurrentNumber, IncomingEndNumber, IncomingPrefix, IncomingSuffix,
                                OutgoingStartNumber, OutgoingCurrentNumber, OutgoingEndNumber, OutgoingPrefix, OutgoingSuffix,
                                InternalStartNumber, InternalCurrentNumber, InternalEndNumber, InternalPrefix, InternalSuffix,
                                ProtocolNumberFormat, NumberPadding, AutoResetYearly, AllowManualEdit, ShowYearInNumber, UseSeparatorSlash,
                                InstitutionName, InstitutionCode, InstitutionAddress, InstitutionPhone, InstitutionEmail, InstitutionWebsite,
                                FiscalYearStart, FiscalYearEnd, Notes, IsActive, IsClosed, CreatedDate, CreatedBy)
                                VALUES 
                                (@Year, @IncomingStartNumber, @IncomingCurrentNumber, @IncomingEndNumber, @IncomingPrefix, @IncomingSuffix,
                                @OutgoingStartNumber, @OutgoingCurrentNumber, @OutgoingEndNumber, @OutgoingPrefix, @OutgoingSuffix,
                                @InternalStartNumber, @InternalCurrentNumber, @InternalEndNumber, @InternalPrefix, @InternalSuffix,
                                @ProtocolNumberFormat, @NumberPadding, @AutoResetYearly, @AllowManualEdit, @ShowYearInNumber, @UseSeparatorSlash,
                                @InstitutionName, @InstitutionCode, @InstitutionAddress, @InstitutionPhone, @InstitutionEmail, @InstitutionWebsite,
                                @FiscalYearStart, @FiscalYearEnd, @Notes, @IsActive, @IsClosed, @CreatedDate, @CreatedBy)";

                            using (var command = new SqlCommand(queryInsert, connection))
                            {
                                AddProtocolSettingsParameters(command, settings);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Përditëso ekzistuesen
                            settings.ModifiedDate = DateTime.Now;
                            settings.ModifiedBy = User.Identity?.Name ?? "System";

                            var queryUpdate = @"UPDATE ProtocolSettings SET
                                Year = @Year,
                                IncomingStartNumber = @IncomingStartNumber,
                                IncomingCurrentNumber = @IncomingCurrentNumber,
                                IncomingEndNumber = @IncomingEndNumber,
                                IncomingPrefix = @IncomingPrefix,
                                IncomingSuffix = @IncomingSuffix,
                                OutgoingStartNumber = @OutgoingStartNumber,
                                OutgoingCurrentNumber = @OutgoingCurrentNumber,
                                OutgoingEndNumber = @OutgoingEndNumber,
                                OutgoingPrefix = @OutgoingPrefix,
                                OutgoingSuffix = @OutgoingSuffix,
                                InternalStartNumber = @InternalStartNumber,
                                InternalCurrentNumber = @InternalCurrentNumber,
                                InternalEndNumber = @InternalEndNumber,
                                InternalPrefix = @InternalPrefix,
                                InternalSuffix = @InternalSuffix,
                                ProtocolNumberFormat = @ProtocolNumberFormat,
                                NumberPadding = @NumberPadding,
                                AutoResetYearly = @AutoResetYearly,
                                AllowManualEdit = @AllowManualEdit,
                                ShowYearInNumber = @ShowYearInNumber,
                                UseSeparatorSlash = @UseSeparatorSlash,
                                InstitutionName = @InstitutionName,
                                InstitutionCode = @InstitutionCode,
                                InstitutionAddress = @InstitutionAddress,
                                InstitutionPhone = @InstitutionPhone,
                                InstitutionEmail = @InstitutionEmail,
                                InstitutionWebsite = @InstitutionWebsite,
                                FiscalYearStart = @FiscalYearStart,
                                FiscalYearEnd = @FiscalYearEnd,
                                Notes = @Notes,
                                ModifiedDate = @ModifiedDate,
                                ModifiedBy = @ModifiedBy
                                WHERE ProtocolSettingsId = @ProtocolSettingsId";

                            using (var command = new SqlCommand(queryUpdate, connection))
                            {
                                AddProtocolSettingsParameters(command, settings);
                                command.Parameters.AddWithValue("@ProtocolSettingsId", settings.ProtocolSettingsId);
                                command.Parameters.AddWithValue("@ModifiedDate", (object)settings.ModifiedDate ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ModifiedBy", (object)settings.ModifiedBy ?? DBNull.Value);

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }

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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if setting exists and is not closed
                    var queryCheck = "SELECT IsClosed FROM ProtocolSettings WHERE ProtocolSettingsId = @Id";
                    using (var command = new SqlCommand(queryCheck, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        var result = await command.ExecuteScalarAsync();

                        if (result == null)
                        {
                            TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                            return RedirectToAction(nameof(Index));
                        }

                        bool isClosed = (bool)result;
                        if (isClosed)
                        {
                            TempData["ErrorMessage"] = "Nuk mund të aktivizohen parametra të mbyllur!";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // Çaktivizo të gjitha
                    var queryDeactivate = @"UPDATE ProtocolSettings SET 
                        IsActive = 0, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy";

                    using (var command = new SqlCommand(queryDeactivate, connection))
                    {
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        await command.ExecuteNonQueryAsync();
                    }

                    // Aktivizo të zgjedhurën
                    var queryActivate = @"UPDATE ProtocolSettings SET 
                        IsActive = 1, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE ProtocolSettingsId = @Id";

                    using (var command = new SqlCommand(queryActivate, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        await command.ExecuteNonQueryAsync();
                    }
                }

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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get setting
                    var queryGet = "SELECT IsActive, IsClosed FROM ProtocolSettings WHERE ProtocolSettingsId = @Id";
                    using (var command = new SqlCommand(queryGet, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                                return RedirectToAction(nameof(Index));
                            }

                            bool isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                            bool isClosed = reader.GetBoolean(reader.GetOrdinal("IsClosed"));

                            if (isActive)
                            {
                                TempData["ErrorMessage"] = "Nuk mund të mbyllen parametrat aktive! Aktivizo një tjetër para se të mbyllësh.";
                                return RedirectToAction(nameof(Index));
                            }

                            if (isClosed)
                            {
                                TempData["ErrorMessage"] = "Parametrat janë të mbyllur tashmë!";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                    }

                    // Close setting
                    var queryUpdate = @"UPDATE ProtocolSettings SET 
                        IsClosed = 1, 
                        ClosedDate = @ClosedDate, 
                        ClosedBy = @ClosedBy,
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE ProtocolSettingsId = @Id";

                    using (var command = new SqlCommand(queryUpdate, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@ClosedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ClosedBy", User.Identity?.Name ?? "System");
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        await command.ExecuteNonQueryAsync();
                    }
                }

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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get setting
                    var queryGet = "SELECT Year, IsActive FROM ProtocolSettings WHERE ProtocolSettingsId = @Id";
                    int year;
                    bool isActive;

                    using (var command = new SqlCommand(queryGet, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                TempData["ErrorMessage"] = "Parametrat nuk u gjetën!";
                                return RedirectToAction(nameof(Index));
                            }

                            year = reader.GetInt32(reader.GetOrdinal("Year"));
                            isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                        }
                    }

                    if (isActive)
                    {
                        TempData["ErrorMessage"] = "Nuk mund të fshihen parametrat aktive! Aktivizo një tjetër para se të fshish.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Check for related documents
                    var queryCheckDocs = "SELECT COUNT(*) FROM Documents WHERE YEAR(ProtocolDate) = @Year";
                    using (var command = new SqlCommand(queryCheckDocs, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);
                        var result = await command.ExecuteScalarAsync();
                        int docCount = result != null ? Convert.ToInt32(result) : 0;

                        if (docCount > 0)
                        {
                            TempData["ErrorMessage"] = "Nuk mund të fshihen parametrat sepse ka dokumente të regjistruara me këto parametra!";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // Delete setting
                    var queryDelete = "DELETE FROM ProtocolSettings WHERE ProtocolSettingsId = @Id";
                    using (var command = new SqlCommand(queryDelete, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }

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
                var previewNumber = _protocolNumberService.FormatProtocolNumber(prefix, number, year, format, padding, showYear);
                return Json(new { success = true, protocolNumber = previewNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method për parameters
        private void AddProtocolSettingsParameters(SqlCommand command, ProtocolSettings settings)
        {
            command.Parameters.AddWithValue("@Year", settings.Year);
            command.Parameters.AddWithValue("@IncomingStartNumber", settings.IncomingStartNumber);
            command.Parameters.AddWithValue("@IncomingCurrentNumber", settings.IncomingCurrentNumber);
            command.Parameters.AddWithValue("@IncomingEndNumber", (object)settings.IncomingEndNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@IncomingPrefix", (object)settings.IncomingPrefix ?? DBNull.Value);
            command.Parameters.AddWithValue("@IncomingSuffix", (object)settings.IncomingSuffix ?? DBNull.Value);
            command.Parameters.AddWithValue("@OutgoingStartNumber", settings.OutgoingStartNumber);
            command.Parameters.AddWithValue("@OutgoingCurrentNumber", settings.OutgoingCurrentNumber);
            command.Parameters.AddWithValue("@OutgoingEndNumber", (object)settings.OutgoingEndNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@OutgoingPrefix", (object)settings.OutgoingPrefix ?? DBNull.Value);
            command.Parameters.AddWithValue("@OutgoingSuffix", (object)settings.OutgoingSuffix ?? DBNull.Value);
            command.Parameters.AddWithValue("@InternalStartNumber", settings.InternalStartNumber);
            command.Parameters.AddWithValue("@InternalCurrentNumber", settings.InternalCurrentNumber);
            command.Parameters.AddWithValue("@InternalEndNumber", (object)settings.InternalEndNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@InternalPrefix", (object)settings.InternalPrefix ?? DBNull.Value);
            command.Parameters.AddWithValue("@InternalSuffix", (object)settings.InternalSuffix ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProtocolNumberFormat", (object)settings.ProtocolNumberFormat ?? DBNull.Value);
            command.Parameters.AddWithValue("@NumberPadding", settings.NumberPadding);
            command.Parameters.AddWithValue("@AutoResetYearly", settings.AutoResetYearly);
            command.Parameters.AddWithValue("@AllowManualEdit", settings.AllowManualEdit);
            command.Parameters.AddWithValue("@ShowYearInNumber", settings.ShowYearInNumber);
            command.Parameters.AddWithValue("@UseSeparatorSlash", settings.UseSeparatorSlash);
            command.Parameters.AddWithValue("@InstitutionName", (object)settings.InstitutionName ?? DBNull.Value);
            command.Parameters.AddWithValue("@InstitutionCode", (object)settings.InstitutionCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@InstitutionAddress", (object)settings.InstitutionAddress ?? DBNull.Value);
            command.Parameters.AddWithValue("@InstitutionPhone", (object)settings.InstitutionPhone ?? DBNull.Value);
            command.Parameters.AddWithValue("@InstitutionEmail", (object)settings.InstitutionEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@InstitutionWebsite", (object)settings.InstitutionWebsite ?? DBNull.Value);
            command.Parameters.AddWithValue("@FiscalYearStart", (object)settings.FiscalYearStart ?? DBNull.Value);
            command.Parameters.AddWithValue("@FiscalYearEnd", (object)settings.FiscalYearEnd ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notes", (object)settings.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", settings.IsActive);
            command.Parameters.AddWithValue("@IsClosed", settings.IsClosed);
            command.Parameters.AddWithValue("@CreatedDate", settings.CreatedDate);
            command.Parameters.AddWithValue("@CreatedBy", (object)settings.CreatedBy ?? DBNull.Value);
        }
    }
}