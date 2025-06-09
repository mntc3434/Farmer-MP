using System.Collections.Generic;
using Farmer_MP.Models;


namespace Farmer_MP.ViewModel
{
    public class FarmerDashboardViewModel
    {
        public List<Product> Products { get; set; }
        public List<Order> Orders { get; set; }
        public decimal TotalIncome { get; set; }
        public int NewOrders { get; set; }
        public int NewMessages { get; set; }
    }
}
