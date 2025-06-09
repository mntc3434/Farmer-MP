using Farmer_MP.Models;
using System.Collections.Generic;

namespace Farmer_MP.ViewModel
{
    public class ShoppingCartViewModel
    {
        // List of shopping cart items
        public List<ShoppingCart> CartItems { get; set; }

        // Optional: Total price of all cart items
        public decimal TotalCartPrice
        {
            get
            {
                return CartItems?.Sum(item => item.TotalPrice) ?? 0;
            }
        }
    }
}
