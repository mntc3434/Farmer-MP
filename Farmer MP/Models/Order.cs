using Farmer_MP.Models;
using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        // Foreign key to Buyer
        public int BuyerID { get; set; }
        public Buyer Buyer { get; set; } // Navigation property to Buyer

        // Foreign key to Product
        public int ProductID { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public float TotalPrice { get; set; }

        [Required]
        public string OrderStatus { get; set; } // e.g., "Pending", "Confirmed", "Shipped", "Delivered"

        [Required]
        public string PaymentStatus { get; set; } // e.g., "Pending", "Paid"

        public DateTime OrderDate { get; set; }
    }
}
