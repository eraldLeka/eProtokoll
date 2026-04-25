using eProtokoll.Repositories;

namespace eProtokoll.Models
{
    public class ReportDashboardViewModel
    {
        // ================= TOTALS =================
        public int TotalDocuments { get; set; }
        public int TotalIncomingDocuments { get; set; }
        public int TotalOutgoingDocuments { get; set; }
        public int TotalInternalDocuments { get; set; }
        public int TotalUsers { get; set; }
        public int TotalInstitutions { get; set; }

        // ================= TIME BASED =================
        public int TodayDocuments { get; set; }
        public int CurrentMonthDocuments { get; set; }
        public int CurrentWeekDocuments { get; set; }

        // ================= PRIORITY =================
        public int HighPriority { get; set; }
        public int NormalPriority { get; set; }
        public int LowPriority { get; set; }

        // ================= TOP LISTS =================
        public List<TopInstitution> TopInstitutions { get; set; } = new();
        public List<TopUser> TopUsers { get; set; } = new();
    }
}