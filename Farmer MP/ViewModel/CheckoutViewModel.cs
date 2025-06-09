using Farmer_MP.Models;

namespace Farmer_MP.ViewModel
{
    public class CheckoutViewModel
    {
        public int BuyerID { get; set; }
        public List<ShoppingCart> CartItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

