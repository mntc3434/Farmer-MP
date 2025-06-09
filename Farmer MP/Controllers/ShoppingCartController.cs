using Farmer_MP.Data;
using Farmer_MP.Models;
using Farmer_MP.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Farmer_MP.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShoppingCartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // View Shopping Cart
        public IActionResult Index(int buyerId)
        {
            // Fetch the buyer by ID
            var buyer = _context.Users.OfType<Buyer>().FirstOrDefault(u => u.UserID == buyerId);

            if (buyer == null)
            {
                return NotFound();
            }

            // Fetch active cart items for the buyer
            var cartItems = _context.ShoppingCarts
                .Where(c => c.BuyerID == buyer.UserID && c.IsActive)
                .Include(c => c.Product) // Ensure that the Product details are loaded
                .ToList();

            // Prepare the view model
            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cartItems
            };

            return View(viewModel);
        }

        // Add Item to Cart
        [HttpPost]
        public IActionResult AddToCart(int buyerId, int productId, int quantity)
        {
            // Check if the product is already in the cart
            var existingCartItem = _context.ShoppingCarts
                .FirstOrDefault(c => c.BuyerID == buyerId && c.ProductID == productId && c.IsActive);

            if (existingCartItem != null)
            {
                // If it exists, update the quantity
                existingCartItem.Quantity += quantity;
                _context.Update(existingCartItem);
            }
            else
            {
                // Add new cart item
                var cartItem = new ShoppingCart
                {
                    BuyerID = buyerId,
                    ProductID = productId,
                    Quantity = quantity,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.ShoppingCarts.Add(cartItem);
            }

            _context.SaveChanges();

            // Return success response
            return Ok(new { success = true });
        }


        // Remove Item from Cart
        public IActionResult RemoveFromCart(int cartId, int buyerId)
        {
            var cartItem = _context.ShoppingCarts.FirstOrDefault(c => c.CartID == cartId && c.BuyerID == buyerId);

            if (cartItem != null)
            {
                cartItem.IsActive = false; // Mark item as inactive instead of deleting it
                _context.Update(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { buyerId });
        }

        // Update Cart Item Quantity
        public IActionResult UpdateQuantity(int cartId, int newQuantity, int buyerId)
        {
            var cartItem = _context.ShoppingCarts.FirstOrDefault(c => c.CartID == cartId && c.BuyerID == buyerId);

            if (cartItem != null && newQuantity > 0)
            {
                cartItem.Quantity = newQuantity;
                _context.Update(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", new { buyerId });
        }

        // Proceed to Checkout
        //public IActionResult Checkout(int buyerId)
        //{
        //    // Fetch the buyer and cart items
        //    var buyer = _context.Users.OfType<Buyer>().FirstOrDefault(u => u.UserID == buyerId);

        //    if (buyer == null)
        //    {
        //        return NotFound();
        //    }

        //    var cartItems = _context.ShoppingCarts
        //        .Where(c => c.BuyerID == buyer.UserID && c.IsActive)
        //        .Include(c => c.Product) // Include product details
        //        .ToList();

        //    // If cart is empty, redirect to the shopping cart
        //    if (!cartItems.Any())
        //    {
        //        return RedirectToAction("Index", new { buyerId });
        //    }

        //    // Logic for processing checkout (e.g., creating an order)
        //    // For now, let's assume the cart is cleared after checkout

        //    // Create Order (Assuming you have an Order model to create)
        //    var order = new Order
        //    {
        //        BuyerID = buyer.UserID,
        //        OrderDate = DateTime.Now,
        //        TotalAmount = cartItems.Sum(item => item.TotalPrice)
        //    };

        //    _context.Orders.Add(order);
        //    _context.SaveChanges();

        //    // Mark cart items as inactive (cart cleared)
        //    foreach (var item in cartItems)
        //    {
        //        item.IsActive = false;
        //        _context.Update(item);
        //    }

        //    _context.SaveChanges();

        //    // Redirect to order confirmation page (you would need to implement this)
        //    return RedirectToAction("OrderConfirmation", "Buyer", new { buyerId = buyer.UserID, orderId = order.OrderID });
        //}

        // Order Confirmation (Example action, you would need to implement the view)
        //public IActionResult OrderConfirmation(int buyerId, int orderId)
        //{
        //    var order = _context.Orders.Include(o => o.OrderItems)
        //                               .ThenInclude(oi => oi.Product)
        //                               .FirstOrDefault(o => o.OrderID == orderId && o.BuyerID == buyerId);

        //    if (order == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(order); // Display the order confirmation view
        //}
    }
}
