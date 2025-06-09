using Farmer_MP.Data;
using Farmer_MP.Models;
using Farmer_MP.ViewModel;
using Farmer_MP.ViewModels; // Corrected namespaces
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;

namespace Farmer_MP.Controllers
{
    public class BuyerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuyerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard View
        public IActionResult BuyerDashboard()
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = _context.Users.OfType<Buyer>().FirstOrDefault(u => u.UserID == buyerId.Value);

            if (buyer == null)
            {
                return NotFound();
            }

            var orders = _context.Orders
                .Include(o => o.Product)
                .ThenInclude(p => p.Farmer)
                .Where(o => o.BuyerID == buyerId)
                .ToList();

            var favoriteCount = _context.Favorites.Count(f => f.UserID == buyerId.Value);

            var viewModel = new BuyerDashboardViewModel
            {
                BuyerID = buyer.UserID,
                BuyerName = buyer.Name,
                Email = buyer.Email,
                Phone = buyer.Phone,
                Orders = orders,
                PurchasedProducts = orders.Select(o => o.Product).ToList(),
                TotalOrders = orders.Count(),
                PendingOrders = orders.Count(o => o.OrderStatus == "Pending"),
                TotalTransactions = (decimal)orders.Sum(o => o.TotalPrice),
                FavoriteCount = favoriteCount // ✅ Add this
            };

            return View(viewModel);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear the session
            return RedirectToAction("Index", "Home");
        }

        // Profile View
        public IActionResult Profile()
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = _context.Users.OfType<Buyer>().FirstOrDefault(u => u.UserID == buyerId);

            if (buyer == null)
            {
                return NotFound();
            }

            var profileViewModel = new ProfileViewModel
            {
                Name = buyer.Name,
                Email = buyer.Email,
                Phone = buyer.Phone,
                Address = buyer.Address,
            };

            return View(profileViewModel);
        }


        // ShoppingCart View
        public IActionResult ShoppingCart(int buyerId)
        {
            var buyer = _context.Users.OfType<Buyer>().FirstOrDefault(u => u.UserID == buyerId);

            if (buyer == null)
            {
                return NotFound();
            }

            var cartItems = _context.ShoppingCarts
                .Where(c => c.BuyerID == buyer.UserID && c.IsActive)
                .Include(c => c.Product)
                .ToList();

            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cartItems
            };

            return View(viewModel);
        }

        public IActionResult OrderHistory()
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");
            if (buyerId == null) return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Where(o => o.BuyerID == buyerId.Value)
                .Include(o => o.Product)
                .ThenInclude(p => p.Farmer) // Ensure farmer is included
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var reviews = _context.Reviews
                .Where(r => r.BuyerID == buyerId.Value)
                .ToList();

            return View(new OrderHistoryViewModel
            {
                Orders = orders,
                ExistingReviews = reviews
            });
        }


        // Messages View
        public IActionResult Messages()
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var buyer = _context.Users.OfType<Buyer>().FirstOrDefault(u => u.UserID == buyerId.Value);
            if (buyer == null)
            {
                return NotFound();
            }

            var messages = _context.Messages
                .Where(m => m.ReceiverID == buyer.UserID || m.SenderID == buyer.UserID)
                .OrderByDescending(m => m.Timestamp)
                .ToList();

            var viewModel = new MessageViewModel
            {
                Messages = messages,
                NewMessage = new Message()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Settings()
        {
            var buyer = _context.Users.FirstOrDefault(u => u.UserType == "Buyer");

            if (buyer == null)
            {
                return NotFound();
            }
            var model = new BuyerSettingsViewModel
            {
                UserID = buyer.UserID,
                Name = buyer.Name,
                Email = buyer.Email,
                Phone = buyer.Phone,
                Address = buyer.Address
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(BuyerSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var buyer = _context.Users.FirstOrDefault(u => u.UserType == "Buyer");

            if (buyer == null)
            {
                return NotFound();
            }

            // Update profile information
            buyer.Name = model.Name;
            buyer.Email = model.Email;
            buyer.Phone = model.Phone;
            buyer.Address = model.Address;

            // Password change section
            if (!string.IsNullOrWhiteSpace(model.CurrentPassword) &&
                !string.IsNullOrWhiteSpace(model.NewPassword) &&
                !string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
            {
                // Validate current password
                if (!VerifyPassword(model.CurrentPassword, buyer.Password))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Validate new password match
                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "New password and confirmation do not match.");
                    return View(model);
                }

                // Hash and update new password
                buyer.Password = HashPassword(model.NewPassword);
            }

            _context.Update(buyer);
            _context.SaveChanges();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Settings");
        }
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            string[] parts = storedPassword.Split(':');
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            byte[] enteredHash = KeyDerivation.Pbkdf2(enteredPassword, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
            return storedHash.SequenceEqual(enteredHash);
        }
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ProductID == productId && f.UserID == userId);

            if (existing != null)
            {
                _context.Favorites.Remove(existing);
            }
            else
            {
                var favorite = new Favorite
                {
                    ProductID = productId,
                    UserID = userId.Value
                };
                _context.Favorites.Add(favorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("BuyerDashboard");
        }
        public async Task<IActionResult> Favorites()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favoriteProducts = await _context.Favorites
                .Where(f => f.UserID == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images) // if applicable
                .Select(f => f.Product)
                .ToListAsync();

            var viewModel = favoriteProducts.Select(p => new ProductViewModel
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Availability = p.Availability,
                Category = p.Category,
                ImageUrls = p.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(), // or adapt if you use something else
                FarmerName = p.Farmer?.Name // assuming Product has a Farmer navigation property
            }).ToList();

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserID == userId && f.ProductID == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult Add(int productId, string comment, int rating)
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingReview = _context.Reviews
                .FirstOrDefault(r => r.ProductID == productId && r.BuyerID == buyerId.Value);

            if (existingReview != null)
            {
                // Update existing review
                existingReview.Comment = comment;
                existingReview.Rating = rating;
                existingReview.Timestamp = DateTime.Now;
                _context.Reviews.Update(existingReview);
            }
            else
            {
                // Add new review
                var review = new Review
                {
                    ProductID = productId,
                    BuyerID = buyerId.Value,
                    Comment = comment,
                    Rating = rating,
                    Timestamp = DateTime.Now
                };

                _context.Reviews.Add(review);
            }

            _context.SaveChanges();

            return RedirectToAction("OrderHistory");
        }

        [HttpPost]
        public IActionResult RemoveDeliveredOrder(int orderId)
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .FirstOrDefault(o => o.OrderID == orderId && o.BuyerID == buyerId.Value);

            if (order == null)
            {
                return NotFound();
            }

            // Get related payments
            var relatedPayments = _context.Payments.Where(p => p.OrderID == orderId).ToList();

            // Remove payments first
            _context.Payments.RemoveRange(relatedPayments);

            // Remove the order
            _context.Orders.Remove(order);
            _context.SaveChanges();

            return RedirectToAction("OrderHistory");
        }



    }
}
