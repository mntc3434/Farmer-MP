using System.Collections.Generic;
using Farmer_MP.Models;

namespace Farmer_MP.ViewModel
{
    public class OrderHistoryViewModel
    {
        public List<Order> Orders { get; set; }
        public List<Review> ExistingReviews { get; set; } // Add this
    }

}
