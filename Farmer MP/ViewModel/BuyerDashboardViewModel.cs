using System;
using System.Collections.Generic;
using Farmer_MP.Models;

namespace Farmer_MP.ViewModel
{
    public class BuyerDashboardViewModel
    {
        public int BuyerID { get; set; }
        public string BuyerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<Order> Orders { get; set; }
        public List<Product> PurchasedProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        //public List<Favorite> FavoriteProducts { get; set; } // Assuming a Favorite model
        public decimal TotalTransactions { get; set; }
        public int FavoriteCount { get; set; }

    }

}

