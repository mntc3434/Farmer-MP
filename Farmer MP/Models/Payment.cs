using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        // Foreign key to Order
        public int OrderID { get; set; }
        public Order Order { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // e.g., "Telebirr", "Bank Transfer"

        [Required]
        public float Amount { get; set; }

        [Required]
        public string TransactionID { get; set; } // Unique transaction ID

        public DateTime PaymentDate { get; set; }
    }
}
