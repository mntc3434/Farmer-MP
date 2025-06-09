using Microsoft.AspNetCore.Mvc;
using Farmer_MP.Models;
using Farmer_MP.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChapaNET;
using System.Collections.Generic;

namespace Farmer_MP.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string chapaSecretKey = "CHASECK_TEST-UmHRBqUAK9Cd8JkK09mc5B76Z6TsReen";
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment()
        {
            try
            {
                int? buyerId = HttpContext.Session.GetInt32("UserID");

                if (buyerId == null)
                {
                    TempData["Error"] = "Please log in to proceed with payment.";
                    return RedirectToAction("Login", "Account");
                }

                // Get complete buyer information
                var buyer = await _context.Users.OfType<Buyer>()
                    .FirstOrDefaultAsync(b => b.UserID == buyerId);

                if (buyer == null)
                {
                    TempData["Error"] = "User information not found.";
                    return RedirectToAction("Cart", "Home");
                }

                // Get all active cart items with product details
                var cartItems = await _context.ShoppingCarts
                    .Where(c => c.BuyerID == buyerId && c.IsActive)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Farmer)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your shopping cart is empty.";
                    return RedirectToAction("Cart", "Home");
                }

                // Calculate total amount
                decimal totalAmount = cartItems.Sum(c => c.TotalPrice);
                var txRef = Chapa.GetUniqueRef();

                // Initialize Chapa
                Chapa chapa = new(chapaSecretKey);

                // Process name (split into first and last names)
                var nameParts = buyer.Name?.Split(' ') ?? new[] { "Customer" };
                var firstName = nameParts.Length > 0 ? nameParts[0] : "Customer";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : " ";

                // Create Chapa payment request
                var request = new ChapaRequest(
                    amount: (int)Math.Round(totalAmount),
                    email: buyer.Email,
                    firstName: firstName,
                    lastName: lastName,
                    tx_ref: txRef,
                    phoneNo: buyer.Phone,
                    currency: "ETB",
                    callback_url: Url.Action("VerifyTransaction", "Payment", null, Request.Scheme),
                    return_url: Url.Action("VerifyTransaction", "Payment", new { txRef = txRef }, Request.Scheme),
                    customTitle: "Farmers Market Payment",
                    customDescription: $"Payment for {cartItems.Count} items from Farmers Market"
                );

                // Send request to Chapa
                var result = await chapa.RequestAsync(request);

                if (result != null && result.Status == "success" && !string.IsNullOrEmpty(result.CheckoutUrl))
                {
                    return Redirect(result.CheckoutUrl);
                }

                TempData["Error"] = "Could not initialize payment. Please try again.";
                return RedirectToAction("Cart", "Home");
            }
            catch (Exception ex)
            {
                // Log the error (you should implement proper logging here)
                Console.WriteLine($"Payment error: {ex.Message}");
                TempData["Error"] = "An error occurred during payment processing.";
                return RedirectToAction("Cart", "Home");
            }
        }

        public async Task<IActionResult> VerifyTransaction(string txRef)
        {
            try
            {
                if (string.IsNullOrEmpty(txRef))
                {
                    ViewBag.Message = "Invalid transaction reference.";
                    return View("VerifyPayment");
                }

                Chapa chapa = new(chapaSecretKey);
                var result = await chapa.VerifyAsync(txRef);

                if (result == null || result.status != "success")
                {
                    ViewBag.Message = "Payment verification failed or payment was not successful.";
                    return View("VerifyPayment");
                }

                var buyerEmail = result.data?.email;
                if (string.IsNullOrEmpty(buyerEmail))
                {
                    ViewBag.Message = "Buyer email missing in transaction.";
                    return View("VerifyPayment");
                }

                // Get buyer information
                var buyer = await _context.Users.OfType<Buyer>()
                    .FirstOrDefaultAsync(b => b.Email == buyerEmail);

                if (buyer == null)
                {
                    ViewBag.Message = "Buyer not found in our system.";
                    return View("VerifyPayment");
                }

                // Get cart items for the buyer
                var cartItems = await _context.ShoppingCarts
                    .Where(c => c.BuyerID == buyer.UserID && c.IsActive)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Farmer)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    ViewBag.Message = "No items found in cart.";
                    return View("VerifyPayment");
                }

                // Create orders for each cart item
                var orders = new List<Order>();
                foreach (var item in cartItems)
                {
                    var order = new Order
                    {
                        BuyerID = buyer.UserID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        TotalPrice = (float)item.TotalPrice,
                        OrderStatus = "Confirmed",
                        PaymentStatus = "Paid",
                        OrderDate = DateTime.Now
                    };
                    orders.Add(order);
                    _context.Orders.Add(order);
                }

                await _context.SaveChangesAsync();

                // Create payment records
                var payments = new List<Payment>();
                foreach (var order in orders)
                {
                    var item = cartItems.First(ci => ci.ProductID == order.ProductID);

                    var payment = new Payment
                    {
                        OrderID = order.OrderID,
                        PaymentMethod = "Chapa",
                        Amount = (float)item.TotalPrice,
                        TransactionID = txRef,
                        PaymentDate = DateTime.Now
                    };
                    payments.Add(payment);

                    // Optional: Notify farmer (you can implement this)
                    var farmer = item.Product?.Farmer;
                    if (farmer != null)
                    {
                        // Implement your notification logic here
                        Console.WriteLine($"Payment received for product {item.Product.Name} from farmer {farmer.Name}");
                    }
                }

                _context.Payments.AddRange(payments);

                // Mark cart items as inactive
                foreach (var item in cartItems)
                {
                    item.IsActive = false;
                }

                await _context.SaveChangesAsync();

                // Redirect to order history with success message
                TempData["Success"] = "Payment successful! Your order has been placed.";
                return RedirectToAction("OrderHistory", "Buyer");
            }
            catch (Exception ex)
            {
                // Log the error (you should implement proper logging here)
                Console.WriteLine($"Verification error: {ex.Message}");
                ViewBag.Message = "An error occurred during payment verification.";
                return View("VerifyPayment");
            }
        }
    }
}