using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using eProtokoll.Models;

namespace eProtokoll.Areas.Employee.Controllers
{
    [Area("Employee")]
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
            // DEVELOPMENT: Hardcoded user ID për testing
            var userId = "test-user-id"; // TODO: Ndrysho me UserId real

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Statistics ONLY - NO RECENT TASKS
                ViewBag.PendingTasks = await GetTaskCountByStatus(connection, userId, TrackingStatus.Assigned);
                ViewBag.InProgressTasks = await GetTaskCountByStatus(connection, userId, TrackingStatus.InProgress);
                ViewBag.CompletedThisWeek = await GetCompletedThisWeek(connection, userId);
                ViewBag.OverdueTasks = await GetOverdueCount(connection, userId);

                // Stats by ActionType
                ViewBag.ForInformationCount = await GetTaskCountByActionType(connection, userId, ActionType.ForInformation);
                ViewBag.ForResponseCount = await GetTaskCountByActionType(connection, userId, ActionType.ForResponse);
            }

            return View();
        }

        // Helper: Get task count by status
        private async Task<int> GetTaskCountByStatus(SqlConnection connection, string userId, TrackingStatus status)
        {
            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND Status = @Status 
                AND IsActive = 1";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Status", (int)status);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Helper: Get completed tasks this week
        private async Task<int> GetCompletedThisWeek(SqlConnection connection, string userId)
        {
            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND Status = @Status 
                AND CompletedDate >= @WeekAgo";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Status", (int)TrackingStatus.Completed);
                command.Parameters.AddWithValue("@WeekAgo", DateTime.Now.AddDays(-7));

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Helper: Get overdue count
        private async Task<int> GetOverdueCount(SqlConnection connection, string userId)
        {
            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND IsActive = 1
                AND HasDeadline = 1
                AND DueDate < @Today
                AND Status NOT IN (@Completed, @Rejected)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Today", DateTime.Now.Date);
                command.Parameters.AddWithValue("@Completed", (int)TrackingStatus.Completed);
                command.Parameters.AddWithValue("@Rejected", (int)TrackingStatus.Rejected);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Helper: Get task count by ActionType
        private async Task<int> GetTaskCountByActionType(SqlConnection connection, string userId, ActionType actionType)
        {
            var query = @"SELECT COUNT(*) 
                FROM DocumentTrackings 
                WHERE AssignedToUserId = @UserId 
                AND ActionType = @ActionType
                AND IsActive = 1
                AND Status NOT IN (@Completed, @Rejected, @Cancelled)";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ActionType", (int)actionType);
                command.Parameters.AddWithValue("@Completed", (int)TrackingStatus.Completed);
                command.Parameters.AddWithValue("@Rejected", (int)TrackingStatus.Rejected);
                command.Parameters.AddWithValue("@Cancelled", (int)TrackingStatus.Cancelled);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }
    }
}