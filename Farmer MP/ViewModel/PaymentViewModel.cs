using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    // PaymentViewModel.cs
    public class PaymentViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ShoppingCart> CartItems { get; set; }
    }

    // PaymentInputModel.cs (for POST only)
    public class PaymentInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}