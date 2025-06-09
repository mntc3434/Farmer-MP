using Farmer_MP.Data;
using Farmer_MP.Models;
using Farmer_MP.ViewModels;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Farmer_MP.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Helper method to get logged-in admin ID
        private int? GetCurrentAdminId()
        {
            return HttpContext.Session.GetInt32("UserID");
        }

        // ✅ Check if current user is admin
        private bool IsAdmin()
        {
            var userType = HttpContext.Session.GetString("UserType");
            return userType == "Admin";
        }

        // ✅ Admin Dashboard
        public IActionResult AdminDashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var totalUsers = _context.Users.Count(u => u.UserType != "Admin");
            var activeOrders = _context.Orders.Count(o => o.OrderStatus == "Active");
            var totalProducts = _context.Products.Count();

            // Mocked for now — you can later store real logs in the DB
            var recentActivities = new List<string>
            {
                "Minte updated product info",
                "New order placed by Alice",
                "Admin suspended user #123"
            };

            var viewModel = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                ActiveOrders = activeOrders,
                TotalProducts = totalProducts,
                RecentActivities = recentActivities
            };

            return View(viewModel);
        }

        // ✅ Manage Users
        public IActionResult ManageUsers()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var currentAdminId = GetCurrentAdminId();
            if (currentAdminId == null)
                return RedirectToAction("Login", "Account");

            // Exclude the current admin user and also other admins
            var users = _context.Users
                .Where(u => u.UserID != currentAdminId.Value && u.UserType != "Admin")
                .ToList();

            return View(users);
        }

        // ✅ Edit User - Form
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new EditUserViewModel
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address
            };

            return View(viewModel);
        }

        // ✅ Edit User - Save Changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(EditUserViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = _context.Users.Find(model.UserID);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Name = model.Name;
            existingUser.Email = model.Email;
            existingUser.Phone = model.Phone;
            existingUser.Address = model.Address;

            _context.Update(existingUser);
            _context.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        // ✅ Suspend User
        [HttpPost]
        public IActionResult SuspendUser(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.IsEmailVerified = false;
            _context.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        // ✅ Manage Products
        public IActionResult ManageProducts()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var products = _context.Products.ToList();
            return View(products);
        }

        // ✅ Edit Product - Form
        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new EditProductViewModel
            {
                ProductID = product.ProductID,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Availability = product.Availability,
                Category = product.Category
            };

            return View(viewModel);
        }

        // ✅ Edit Product - Save Changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(EditProductViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingProduct = _context.Products.Find(model.ProductID);
            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = model.Name;
            existingProduct.Description = model.Description;
            existingProduct.Price = model.Price;
            existingProduct.Availability = model.Availability;
            existingProduct.Category = model.Category;

            _context.Update(existingProduct);
            _context.SaveChanges();

            return RedirectToAction("ManageProducts");
        }

        // ✅ Remove Product
        public IActionResult RemoveProduct(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            _context.SaveChanges();

            return RedirectToAction("ManageProducts");
        }

        // ✅ View Orders
        public IActionResult Orders()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                .ToList();

            return View(orders);
        }

        // ✅ Analytics
        public IActionResult Analytics()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // ✅ Admin Settings - Form
        [HttpGet]
        public IActionResult Settings()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var adminId = GetCurrentAdminId();
            if (adminId == null)
                return RedirectToAction("Login", "Account");

            var admin = _context.Users.FirstOrDefault(u => u.UserID == adminId.Value);
            if (admin == null)
            {
                return NotFound();
            }

            var model = new AdminSettingsViewModel
            {
                UserID = admin.UserID,
                Name = admin.Name,
                Email = admin.Email,
                Phone = admin.Phone,
                Address = admin.Address
            };

            return View(model);
        }

        // ✅ Admin Settings - Save Changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(AdminSettingsViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = _context.Users.FirstOrDefault(u => u.UserID == model.UserID);
            if (admin == null)
            {
                return NotFound();
            }

            // Update profile information
            admin.Name = model.Name;
            admin.Email = model.Email;
            admin.Phone = model.Phone;
            admin.Address = model.Address;

            // Password change section
            if (!string.IsNullOrWhiteSpace(model.CurrentPassword) &&
                !string.IsNullOrWhiteSpace(model.NewPassword) &&
                !string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
            {
                // Validate current password
                if (!VerifyPassword(model.CurrentPassword, admin.Password))
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
                admin.Password = HashPassword(model.NewPassword);
            }

            _context.Update(admin);
            _context.SaveChanges();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Settings");
        }

        // ✅ Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear the session
            return RedirectToAction("Index", "Home");
        }

        // Helper method to hash the password
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

        // Helper method to verify the password
        private bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            string[] parts = storedPassword.Split(':');
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            byte[] enteredHash = KeyDerivation.Pbkdf2(enteredPassword, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
            return storedHash.SequenceEqual(enteredHash);
        }
    }
}