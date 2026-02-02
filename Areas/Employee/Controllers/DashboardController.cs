using eProtokoll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Employee/Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Statistics
                ViewBag.InProgressTasks = await GetInProgressCount(connection, userId);
                ViewBag.CompletedThisWeek = await GetCompletedThisWeek(connection, userId);
                ViewBag.TotalAssigned = await GetTotalAssignedCount(connection, userId);
                ViewBag.OverdueTasks = await GetOverdueCount(connection, userId);
            }

            return View();
        }

        // Helper: Get in-progress count (not yet completed)
        private async Task<int> GetInProgressCount(SqlConnection connection, string userId)
        {
            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND CompletedDate IS NULL
                AND IsActive = 1";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Helper: Get completed tasks this week
        private async Task<int> GetCompletedThisWeek(SqlConnection connection, string userId)
        {
            var weekAgo = DateTime.Now.AddDays(-7);

            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND CompletedDate IS NOT NULL
                AND CompletedDate >= @WeekAgo";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@WeekAgo", weekAgo);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Helper: Get total assigned count
        private async Task<int> GetTotalAssignedCount(SqlConnection connection, string userId)
        {
            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND IsActive = 1";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Helper: Get overdue count
        private async Task<int> GetOverdueCount(SqlConnection connection, string userId)
        {
            var today = DateTime.Now.Date;

            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND IsActive = 1
                AND DueDate IS NOT NULL
                AND DueDate < @Today
                AND CompletedDate IS NULL";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Today", today);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
    }
}