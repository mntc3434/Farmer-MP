using System.Collections.Generic;

namespace Farmer_MP.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveOrders { get; set; }
        public int TotalProducts { get; set; }
        public List<string> RecentActivities { get; set; } = new List<string>();
    }
}
