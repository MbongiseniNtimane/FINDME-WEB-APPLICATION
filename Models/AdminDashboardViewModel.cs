namespace ASPNETCore_DB.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalLostItems { get; set; }
        public int TotalFoundItems { get; set; }
        public double LostPercentage { get; set; }
        public double FoundPercentage { get; set; }
        public int RecentItemsCount { get; set; }
        public List<MonthlyItemCount> MonthlyItemCounts { get; set; }
        public string AdminEmail { get; set; }
    }

    public class MonthlyItemCount
    {
        public string Month { get; set; }
        public int LostCount { get; set; }
        public int FoundCount { get; set; }
    }
}
