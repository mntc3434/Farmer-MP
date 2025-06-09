using Farmer_MP.Models;
using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        // Foreign key to Buyer
        public int BuyerID { get; set; }
        public Buyer Buyer { get; set; }

        // Foreign key to Product
        public int ProductID { get; set; }
        public Product Product { get; set; }

        public float Rating { get; set; }
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
