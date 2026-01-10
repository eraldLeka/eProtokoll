using Microsoft.AspNetCore.Mvc;
using eProtokoll.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using eProtokoll.Services.Mappers;

namespace eProtokoll.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ClassificationController : Controller
    {
        private readonly string _connectionString;

        public ClassificationController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        // GET: Admin/Classification
        public async Task<IActionResult> Index()
        {
            var classifications = new List<Classification>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT c.*, 
                    (SELECT COUNT(*) FROM Documents WHERE ClassificationId = c.ClassificationId) AS DocumentCount
                    FROM Classifications c 
                    ORDER BY c.SortOrder, c.Level";

                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            classifications.Add(ClassificationMapper.MapToClassification(reader));
                        }
                    }
                }
            }

            return View(classifications);
        }

        // GET: Admin/Classification/Create
        public IActionResult Create()
        {
            var model = new Classification
            {
                IsActive = true,
                AllowPrint = true,
                AllowDownload = true,
                AllowCopy = true,
                EnableAuditLog = true,
                RetentionYears = 5,
                SortOrder = 0
            };

            return View(model);
        }

        // POST: Admin/Classification/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Classification classification)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // Nëse është zgjedhur si default, heq default-in nga të tjerët
                        if (classification.IsDefault)
                        {
                            var queryUnsetDefault = "UPDATE Classifications SET IsDefault = 0 WHERE IsDefault = 1";
                            using (var command = new SqlCommand(queryUnsetDefault, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        classification.CreatedDate = DateTime.Now;
                        classification.CreatedBy = User.Identity?.Name ?? "System";

                        var queryInsert = @"INSERT INTO Classifications 
                            (Name, Level, Description, RetentionYears, RequiresApproval, MinimumRoleRequired, 
                            AllowPrint, AllowDownload, AllowCopy, EnableAuditLog, ColorCode, SortOrder, 
                            IsActive, IsDefault, CreatedDate, CreatedBy)
                            VALUES 
                            (@Name, @Level, @Description, @RetentionYears, @RequiresApproval, @MinimumRoleRequired,
                            @AllowPrint, @AllowDownload, @AllowCopy, @EnableAuditLog, @ColorCode, @SortOrder,
                            @IsActive, @IsDefault, @CreatedDate, @CreatedBy)";

                        using (var command = new SqlCommand(queryInsert, connection))
                        {
                            command.Parameters.AddWithValue("@Name", classification.Name);
                            command.Parameters.AddWithValue("@Level", (int)classification.Level);
                            command.Parameters.AddWithValue("@Description", (object)classification.Description ?? DBNull.Value);
                            command.Parameters.AddWithValue("@RetentionYears", classification.RetentionYears);
                            command.Parameters.AddWithValue("@RequiresApproval", classification.RequiresApproval);
                            command.Parameters.AddWithValue("@MinimumRoleRequired", (object)classification.MinimumRoleRequired ?? DBNull.Value);
                            command.Parameters.AddWithValue("@AllowPrint", classification.AllowPrint);
                            command.Parameters.AddWithValue("@AllowDownload", classification.AllowDownload);
                            command.Parameters.AddWithValue("@AllowCopy", classification.AllowCopy);
                            command.Parameters.AddWithValue("@EnableAuditLog", classification.EnableAuditLog);
                            command.Parameters.AddWithValue("@ColorCode", (object)classification.ColorCode ?? DBNull.Value);
                            command.Parameters.AddWithValue("@SortOrder", classification.SortOrder);
                            command.Parameters.AddWithValue("@IsActive", classification.IsActive);
                            command.Parameters.AddWithValue("@IsDefault", classification.IsDefault);
                            command.Parameters.AddWithValue("@CreatedDate", classification.CreatedDate);
                            command.Parameters.AddWithValue("@CreatedBy", (object)classification.CreatedBy ?? DBNull.Value);

                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    TempData["SuccessMessage"] = $"Klasifikimi '{classification.Name}' u krijua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë ruajtjes: {ex.Message}");
                }
            }

            return View(classification);
        }

        // GET: Admin/Classification/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Classification classification = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Classifications WHERE ClassificationId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id.Value);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            classification = ClassificationMapper.MapToClassification(reader);
                        }
                    }
                }
            }

            if (classification == null)
            {
                return NotFound();
            }

            return View(classification);
        }

        // POST: Admin/Classification/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Classification classification)
        {
            if (id != classification.ClassificationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();

                        // Nëse është zgjedhur si default, heq default-in nga të tjerët
                        if (classification.IsDefault)
                        {
                            var queryUnsetDefault = "UPDATE Classifications SET IsDefault = 0 WHERE IsDefault = 1 AND ClassificationId != @Id";
                            using (var command = new SqlCommand(queryUnsetDefault, connection))
                            {
                                command.Parameters.AddWithValue("@Id", id);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        classification.ModifiedDate = DateTime.Now;
                        classification.ModifiedBy = User.Identity?.Name ?? "System";

                        var queryUpdate = @"UPDATE Classifications SET
                            Name = @Name,
                            Level = @Level,
                            Description = @Description,
                            RetentionYears = @RetentionYears,
                            RequiresApproval = @RequiresApproval,
                            MinimumRoleRequired = @MinimumRoleRequired,
                            AllowPrint = @AllowPrint,
                            AllowDownload = @AllowDownload,
                            AllowCopy = @AllowCopy,
                            EnableAuditLog = @EnableAuditLog,
                            ColorCode = @ColorCode,
                            SortOrder = @SortOrder,
                            IsActive = @IsActive,
                            IsDefault = @IsDefault,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                            WHERE ClassificationId = @ClassificationId";

                        using (var command = new SqlCommand(queryUpdate, connection))
                        {
                            command.Parameters.AddWithValue("@ClassificationId", classification.ClassificationId);
                            command.Parameters.AddWithValue("@Name", classification.Name);
                            command.Parameters.AddWithValue("@Level", (int)classification.Level);
                            command.Parameters.AddWithValue("@Description", (object)classification.Description ?? DBNull.Value);
                            command.Parameters.AddWithValue("@RetentionYears", classification.RetentionYears);
                            command.Parameters.AddWithValue("@RequiresApproval", classification.RequiresApproval);
                            command.Parameters.AddWithValue("@MinimumRoleRequired", (object)classification.MinimumRoleRequired ?? DBNull.Value);
                            command.Parameters.AddWithValue("@AllowPrint", classification.AllowPrint);
                            command.Parameters.AddWithValue("@AllowDownload", classification.AllowDownload);
                            command.Parameters.AddWithValue("@AllowCopy", classification.AllowCopy);
                            command.Parameters.AddWithValue("@EnableAuditLog", classification.EnableAuditLog);
                            command.Parameters.AddWithValue("@ColorCode", (object)classification.ColorCode ?? DBNull.Value);
                            command.Parameters.AddWithValue("@SortOrder", classification.SortOrder);
                            command.Parameters.AddWithValue("@IsActive", classification.IsActive);
                            command.Parameters.AddWithValue("@IsDefault", classification.IsDefault);
                            command.Parameters.AddWithValue("@ModifiedDate", (object)classification.ModifiedDate ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ModifiedBy", (object)classification.ModifiedBy ?? DBNull.Value);

                            int rowsAffected = await command.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }
                    }

                    TempData["SuccessMessage"] = $"Klasifikimi '{classification.Name}' u përditësua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë përditësimit: {ex.Message}");
                }
            }

            return View(classification);
        }

        // GET: Admin/Classification/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Classification classification = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = @"SELECT c.*, 
                    (SELECT COUNT(*) FROM Documents WHERE ClassificationId = c.ClassificationId) AS DocumentCount
                    FROM Classifications c 
                    WHERE c.ClassificationId = @Id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id.Value);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            classification = ClassificationMapper.MapToClassification(reader);
                        }
                    }
                }
            }

            if (classification == null)
            {
                return NotFound();
            }

            return View(classification);
        }

        // POST: Admin/Classification/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                Classification classification = null;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get classification
                    var queryGet = "SELECT * FROM Classifications WHERE ClassificationId = @Id";
                    using (var command = new SqlCommand(queryGet, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                classification = ClassificationMapper.MapToClassification(reader);
                            }
                        }
                    }

                    if (classification == null)
                    {
                        TempData["ErrorMessage"] = "Klasifikimi nuk u gjet!";
                        return RedirectToAction(nameof(Index));
                    }

                    // Check for related documents
                    var queryCheckDocs = "SELECT COUNT(*) FROM Documents WHERE ClassificationId = @Id";
                    using (var command = new SqlCommand(queryCheckDocs, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        var result = await command.ExecuteScalarAsync();
                        int docCount = result != null ? Convert.ToInt32(result) : 0;

                        if (docCount > 0)
                        {
                            TempData["ErrorMessage"] = $"Nuk mund të fshihet! Klasifikimi '{classification.Name}' përdoret nga {docCount} dokumente.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // Delete classification
                    var queryDelete = "DELETE FROM Classifications WHERE ClassificationId = @Id";
                    using (var command = new SqlCommand(queryDelete, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = $"Klasifikimi '{classification.Name}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Classification/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get current status
                    var queryGet = "SELECT IsActive, Name FROM Classifications WHERE ClassificationId = @Id";
                    bool currentStatus;
                    string name;

                    using (var command = new SqlCommand(queryGet, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return Json(new { success = false, message = "Klasifikimi nuk u gjet!" });
                            }
                            currentStatus = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                            name = reader.GetString(reader.GetOrdinal("Name"));
                        }
                    }

                    // Toggle status
                    var newStatus = !currentStatus;
                    var queryUpdate = @"UPDATE Classifications SET 
                        IsActive = @IsActive, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE ClassificationId = @Id";

                    using (var command = new SqlCommand(queryUpdate, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@IsActive", newStatus);
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");

                        await command.ExecuteNonQueryAsync();
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Statusi u ndryshua me sukses!",
                        isActive = newStatus
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Classification/BulkActivate
        [HttpPost]
        public async Task<IActionResult> BulkActivate([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Nuk ka klasifikime të zgjedhura!" });
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Build parameterized query to prevent SQL injection
                    var parameters = new List<SqlParameter>();
                    var paramNames = new List<string>();

                    for (int i = 0; i < ids.Count; i++)
                    {
                        var paramName = $"@Id{i}";
                        paramNames.Add(paramName);
                        parameters.Add(new SqlParameter(paramName, ids[i]));
                    }

                    var query = $@"UPDATE Classifications SET 
                        IsActive = 1, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE ClassificationId IN ({string.Join(",", paramNames)})";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        command.Parameters.AddRange(parameters.ToArray());

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"{rowsAffected} klasifikime u aktivizuan me sukses!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Classification/BulkDeactivate
        [HttpPost]
        public async Task<IActionResult> BulkDeactivate([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Nuk ka klasifikime të zgjedhura!" });
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Build parameterized query to prevent SQL injection
                    var parameters = new List<SqlParameter>();
                    var paramNames = new List<string>();

                    for (int i = 0; i < ids.Count; i++)
                    {
                        var paramName = $"@Id{i}";
                        paramNames.Add(paramName);
                        parameters.Add(new SqlParameter(paramName, ids[i]));
                    }

                    var query = $@"UPDATE Classifications SET 
                        IsActive = 0, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE ClassificationId IN ({string.Join(",", paramNames)})";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        command.Parameters.AddRange(parameters.ToArray());

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"{rowsAffected} klasifikime u çaktivizuan me sukses!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Classification/BulkDelete
        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Nuk ka klasifikime të zgjedhura!" });
                }

                var withDocuments = new List<string>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check for related documents
                    foreach (var id in ids)
                    {
                        var queryCheckDocs = "SELECT COUNT(*) FROM Documents WHERE ClassificationId = @Id";
                        using (var command = new SqlCommand(queryCheckDocs, connection))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            var result = await command.ExecuteScalarAsync();
                            int docCount = result != null ? Convert.ToInt32(result) : 0;

                            if (docCount > 0)
                            {
                                var queryGetName = "SELECT Name FROM Classifications WHERE ClassificationId = @Id";
                                using (var cmdName = new SqlCommand(queryGetName, connection))
                                {
                                    cmdName.Parameters.AddWithValue("@Id", id);
                                    var name = await cmdName.ExecuteScalarAsync() as string;
                                    if (name != null)
                                        withDocuments.Add(name);
                                }
                            }
                        }
                    }

                    if (withDocuments.Any())
                    {
                        var names = string.Join(", ", withDocuments);
                        return Json(new
                        {
                            success = false,
                            message = $"Këto klasifikime nuk mund të fshihen sepse kanë dokumente të lidhura: {names}"
                        });
                    }

                    // Build parameterized query to prevent SQL injection
                    var parameters = new List<SqlParameter>();
                    var paramNames = new List<string>();

                    for (int i = 0; i < ids.Count; i++)
                    {
                        var paramName = $"@Id{i}";
                        paramNames.Add(paramName);
                        parameters.Add(new SqlParameter(paramName, ids[i]));
                    }

                    var queryDelete = $"DELETE FROM Classifications WHERE ClassificationId IN ({string.Join(",", paramNames)})";

                    using (var command = new SqlCommand(queryDelete, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"{rowsAffected} klasifikime u fshinë me sukses!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // Helper method për të kontrolluar ekzistencën
        private async Task<bool> ClassificationExists(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM Classifications WHERE ClassificationId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    int count = result != null ? Convert.ToInt32(result) : 0;
                    return count > 0;
                }
            }
        }
    }
}