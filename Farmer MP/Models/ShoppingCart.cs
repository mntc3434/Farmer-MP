using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farmer_MP.Models
{
    public class ShoppingCart
    {
        [Key]
        public int CartID { get; set; } // Unique ID for the shopping cart item

        // Foreign key to Buyer (the user who owns the cart)
        [Required]
        public int BuyerID { get; set; }

        [ForeignKey("BuyerID")]
        public virtual Buyer Buyer { get; set; } // Navigation property to the Buyer

        // Foreign key to Product (the product in the cart)
        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } // Navigation property to the Product

        // Quantity of the product in the cart
        [Required]
        public int Quantity { get; set; }

        // Optional: You can add other properties like created timestamp if needed
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Optional: You can also add a boolean to indicate if the item is still in the cart
        public bool IsActive { get; set; } = true;

        // Total price for this particular cart item (calculated based on quantity and product price)
        public decimal TotalPrice
        {
            get
            {
                return (decimal)(Product.Price * Quantity); // Assuming the Product model has a 'Price' field
            }
        }
    }
}
