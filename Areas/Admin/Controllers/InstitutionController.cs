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
    public class InstitutionController : Controller
    {
        private readonly string _connectionString;

        public InstitutionController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Admin/Institution
        public async Task<IActionResult> Index()
        {
            var institutions = new List<Institution>();

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Institutions ORDER BY Name";
                using (var command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            institutions.Add(InstitutionMapper.MapToInstitution(reader));
                        }
                    }
                }
            }

            return View(institutions);
        }

        // GET: Admin/Institution/Create
        public IActionResult Create()
        {
            var model = new Institution
            {
                IsActive = true
            };

            return View(model);
        }

        // POST: Admin/Institution/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Institution institution)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    institution.CreatedDate = DateTime.Now;
                    institution.CreatedBy = User.Identity?.Name ?? "System";

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        var query = @"INSERT INTO Institutions 
                            (Name, ShortName, Type, TaxCode, Adress, City, Country, Phone, Email, Website, ContactPerson, IsActive, CreatedDate, CreatedBy)
                            VALUES 
                            (@Name, @ShortName, @Type, @TaxCode, @Adress, @City, @Country, @Phone, @Email, @Website, @ContactPerson, @IsActive, @CreatedDate, @CreatedBy)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Name", institution.Name);
                            command.Parameters.AddWithValue("@ShortName", (object)institution.ShortName ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Type", (int)institution.Type);
                            command.Parameters.AddWithValue("@TaxCode", (object)institution.TaxCode ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Adress", (object)institution.Adress ?? DBNull.Value);
                            command.Parameters.AddWithValue("@City", (object)institution.City ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Country", (object)institution.Country ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Phone", (object)institution.Phone ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Email", (object)institution.Email ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Website", (object)institution.Website ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ContactPerson", (object)institution.ContactPerson ?? DBNull.Value);
                            command.Parameters.AddWithValue("@IsActive", institution.IsActive);
                            command.Parameters.AddWithValue("@CreatedDate", institution.CreatedDate);
                            command.Parameters.AddWithValue("@CreatedBy", (object)institution.CreatedBy ?? DBNull.Value);

                            await connection.OpenAsync();
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u krijua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë ruajtjes: {ex.Message}");
                }
            }

            return View(institution);
        }

        // GET: Admin/Institution/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Institution institution = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Institutions WHERE InstitutionId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id.Value);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            institution = InstitutionMapper.MapToInstitution(reader);
                        }
                    }
                }
            }

            if (institution == null)
            {
                return NotFound();
            }

            return View(institution);
        }

        // POST: Admin/Institution/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Institution institution)
        {
            if (id != institution.InstitutionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    institution.ModifiedDate = DateTime.Now;
                    institution.ModifiedBy = User.Identity?.Name ?? "System";

                    using (var connection = new SqlConnection(_connectionString))
                    {
                        var query = @"UPDATE Institutions SET
                            Name = @Name,
                            ShortName = @ShortName,
                            Type = @Type,
                            TaxCode = @TaxCode,
                            Adress = @Adress,
                            City = @City,
                            Country = @Country,
                            Phone = @Phone,
                            Email = @Email,
                            Website = @Website,
                            ContactPerson = @ContactPerson,
                            IsActive = @IsActive,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                            WHERE InstitutionId = @InstitutionId";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@InstitutionId", institution.InstitutionId);
                            command.Parameters.AddWithValue("@Name", institution.Name);
                            command.Parameters.AddWithValue("@ShortName", (object)institution.ShortName ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Type", (int)institution.Type);
                            command.Parameters.AddWithValue("@TaxCode", (object)institution.TaxCode ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Adress", (object)institution.Adress ?? DBNull.Value);
                            command.Parameters.AddWithValue("@City", (object)institution.City ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Country", (object)institution.Country ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Phone", (object)institution.Phone ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Email", (object)institution.Email ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Website", (object)institution.Website ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ContactPerson", (object)institution.ContactPerson ?? DBNull.Value);
                            command.Parameters.AddWithValue("@IsActive", institution.IsActive);
                            command.Parameters.AddWithValue("@ModifiedDate", (object)institution.ModifiedDate ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ModifiedBy", (object)institution.ModifiedBy ?? DBNull.Value);

                            await connection.OpenAsync();
                            int rowsAffected = await command.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }
                    }

                    TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u përditësua me sukses!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Gabim gjatë përditësimit: {ex.Message}");
                }
            }

            return View(institution);
        }

        // GET: Admin/Institution/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Institution institution = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Institutions WHERE InstitutionId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id.Value);
                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            institution = InstitutionMapper.MapToInstitution(reader);
                        }
                    }
                }
            }

            if (institution == null)
            {
                return NotFound();
            }

            return View(institution);
        }

        // POST: Admin/Institution/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                Institution institution = null;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get institution
                    var queryGet = "SELECT * FROM Institutions WHERE InstitutionId = @Id";
                    using (var command = new SqlCommand(queryGet, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                institution = InstitutionMapper.MapToInstitution(reader);
                            }
                        }
                    }

                    if (institution == null)
                    {
                        TempData["ErrorMessage"] = "Institucioni nuk u gjet!";
                        return RedirectToAction(nameof(Index));
                    }

                    // Check for related documents (TPH - Documents table with DocumentType filter)
                    var queryCheckDocuments = @"SELECT COUNT(*) FROM Documents 
                        WHERE InstitutionId = @Id AND (DocumentType = 1 OR DocumentType = 2)";
                    using (var command = new SqlCommand(queryCheckDocuments, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int documentCount = (int)await command.ExecuteScalarAsync();

                        if (documentCount > 0)
                        {
                            TempData["ErrorMessage"] = $"Nuk mund të fshihet! Institucioni '{institution.Name}' përdoret nga {documentCount} dokument(e).";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // Delete institution
                    var queryDelete = "DELETE FROM Institutions WHERE InstitutionId = @Id";
                    using (var command = new SqlCommand(queryDelete, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = $"Institucioni '{institution.Name}' u fshi me sukses!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gabim gjatë fshirjes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Institution/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get current status
                    var queryGet = "SELECT IsActive, Name FROM Institutions WHERE InstitutionId = @Id";
                    bool currentStatus;
                    string name;

                    using (var command = new SqlCommand(queryGet, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return Json(new { success = false, message = "Institucioni nuk u gjet!" });
                            }
                            currentStatus = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                            name = reader.GetString(reader.GetOrdinal("Name"));
                        }
                    }

                    // Toggle status
                    var newStatus = !currentStatus;
                    var queryUpdate = @"UPDATE Institutions SET 
                        IsActive = @IsActive, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE InstitutionId = @Id";

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

        // POST: Admin/Institution/BulkActivate
        [HttpPost]
        public async Task<IActionResult> BulkActivate([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Nuk ka institucione të zgjedhura!" });
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

                    var query = $@"UPDATE Institutions SET 
                        IsActive = 1, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE InstitutionId IN ({string.Join(",", paramNames)})";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        command.Parameters.AddRange(parameters.ToArray());

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"{rowsAffected} institucione u aktivizuan me sukses!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Institution/BulkDeactivate
        [HttpPost]
        public async Task<IActionResult> BulkDeactivate([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Nuk ka institucione të zgjedhura!" });
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

                    var query = $@"UPDATE Institutions SET 
                        IsActive = 0, 
                        ModifiedDate = @ModifiedDate, 
                        ModifiedBy = @ModifiedBy 
                        WHERE InstitutionId IN ({string.Join(",", paramNames)})";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@ModifiedBy", User.Identity?.Name ?? "System");
                        command.Parameters.AddRange(parameters.ToArray());

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"{rowsAffected} institucione u çaktivizuan me sukses!"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Gabim: {ex.Message}" });
            }
        }

        // POST: Admin/Institution/BulkDelete
        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Nuk ka institucione të zgjedhura!" });
                }

                var withDocuments = new List<string>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check for related documents (TPH - Documents table)
                    foreach (var id in ids)
                    {
                        var queryCheckDocuments = @"SELECT COUNT(*) FROM Documents 
                            WHERE InstitutionId = @Id AND (DocumentType = 1 OR DocumentType = 2)";
                        using (var command = new SqlCommand(queryCheckDocuments, connection))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            int documentCount = (int)await command.ExecuteScalarAsync();

                            if (documentCount > 0)
                            {
                                var queryGetName = "SELECT Name FROM Institutions WHERE InstitutionId = @Id";
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
                            message = $"Këto institucione nuk mund të fshihen sepse kanë dokumente të lidhura: {names}"
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

                    var queryDelete = $"DELETE FROM Institutions WHERE InstitutionId IN ({string.Join(",", paramNames)})";

                    using (var command = new SqlCommand(queryDelete, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return Json(new
                        {
                            success = true,
                            message = $"{rowsAffected} institucione u fshinë me sukses!"
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
        private async Task<bool> InstitutionExists(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM Institutions WHERE InstitutionId = @Id";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    await connection.OpenAsync();
                    int count = (int)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
    }
}