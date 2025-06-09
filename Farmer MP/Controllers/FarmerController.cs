using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Farmer_MP.Models;
using Farmer_MP.Data;
using Farmer_MP.ViewModel;
using System.Linq;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class FarmersController : Controller
{
    private readonly ApplicationDbContext _context;

    public FarmersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ✅ Helper method to get logged-in farmer ID
    private int? GetCurrentFarmerId()
    {
        return HttpContext.Session.GetInt32("UserID");
    }

    // ✅ Farmer Dashboard
    public IActionResult FarmerDashboard()
    {
        var farmerId = GetCurrentFarmerId();
        if (farmerId == null)
            return RedirectToAction("Login", "Account");

        var products = _context.Products
            .Where(p => p.UserID == farmerId.Value)
            .ToList();

        decimal totalIncome = _context.Orders
            .Where(o => o.Product.UserID == farmerId.Value)
            .Sum(o => (decimal)o.TotalPrice);

        int newOrders = _context.Orders
            .Where(o => o.Product.UserID == farmerId.Value && o.OrderStatus == "Pending")
            .Count();

        int newMessages = _context.Messages
            .Where(m => m.ReceiverID == farmerId.Value && !m.IsRead)
            .Count();

        var orders = _context.Orders
            .Where(o => o.Product.UserID == farmerId.Value)
            .Include(o => o.Product)
            .ToList();

        var viewModel = new FarmerDashboardViewModel
        {
            Products = products,
            TotalIncome = totalIncome,
            NewOrders = newOrders,
            NewMessages = newMessages,
            Orders = orders
        };

        return View(viewModel);
    }

    // ✅ Manage Products - Display List
    public IActionResult ManageProducts()
    {
        var farmerId = GetCurrentFarmerId();
        if (farmerId == null) return RedirectToAction("Login", "Account");

        var products = _context.Products
            .Where(p => p.UserID == farmerId.Value)
            .ToList();

        return View(products);
    }

    // ✅ Create New Product - Form
    public IActionResult CreateProduct()
    {
        return View();
    }

    // ✅ Create New Product - Save to Database
    [HttpPost]
    public IActionResult CreateProduct(Product product)
    {
        if (ModelState.IsValid)
        {
            var userId = GetCurrentFarmerId();
            if (userId == null) return RedirectToAction("Login", "Account");

            product.UserID = userId.Value;
            _context.Products.Add(product);
            _context.SaveChanges();
            return RedirectToAction("ManageProducts");
        }

        return View(product);
    }

    // ✅ Edit Product - Form
    public IActionResult EditProduct(int id)
    {
        var product = _context.Products.Find(id);
        if (product == null)
            return NotFound();

        return View(product);
    }

    // ✅ Edit Product - Save Changes
    [HttpPost]
    public IActionResult EditProduct(Product product)
    {
        if (ModelState.IsValid)
        {
            _context.Products.Update(product);
            _context.SaveChanges();
            return RedirectToAction("ManageProducts");
        }

        return View(product);
    }

    // ✅ Delete Product
    public IActionResult DeleteProduct(int id)
    {
        var product = _context.Products.Find(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        _context.SaveChanges();
        return RedirectToAction("ManageProducts");
    }

    // ✅ Notifications Page (Pending Orders)
    public IActionResult Notifications()
    {
        var farmerId = GetCurrentFarmerId();
        if (farmerId == null) return RedirectToAction("Login", "Account");

        // Get orders for this farmer that should be shown in notifications
        var orders = _context.Orders
            .Where(o => o.Product.UserID == farmerId.Value &&
                       (o.OrderStatus == "Pending" ||
                        o.OrderStatus == "Confirmed" ||
                        o.OrderStatus == "Shipped"))
            .Include(o => o.Product)
            .Include(o => o.Buyer)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        var newMessages = _context.Messages
            .Where(m => m.ReceiverID == farmerId.Value && !m.IsRead)
            .Count();

        var notifications = orders.Select(order => new NotificationItem
        {
            Title = $"Order #{order.OrderID} - {order.Product.Name}",
            Message = $"{order.Buyer.Name} ordered {order.Quantity} x {order.Product.Name} (Total: {order.TotalPrice} Birr)",
            Date = order.OrderDate,
            OrderId = order.OrderID,
            CurrentStatus = order.OrderStatus,
            BuyerName = order.Buyer.Name,
            ProductName = order.Product.Name,
            Quantity = order.Quantity,
            TotalPrice = order.TotalPrice
        }).ToList();

        var viewModel = new FarmerNotificationsViewModel
        {
            NewNotificationsCount = notifications.Count,
            NewMessagesCount = newMessages,
            Notifications = notifications
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOrderStatus(int orderId, string status)
    {
        var farmerId = GetCurrentFarmerId();
        if (farmerId == null) return RedirectToAction("Login", "Account");

        var order = _context.Orders
            .Include(o => o.Product)
            .FirstOrDefault(o => o.OrderID == orderId && o.Product.UserID == farmerId);

        if (order == null)
        {
            TempData["Error"] = "Order not found";
            return RedirectToAction("Notifications");
        }

        // Validate status transition
        switch (order.OrderStatus)
        {
            case "Pending":
            case "Confirmed":
                if (status != "Shipped")
                {
                    TempData["Error"] = $"Cannot change status from {order.OrderStatus} to {status}";
                    return RedirectToAction("Notifications");
                }
                break;
            case "Shipped":
                if (status != "Delivered")
                {
                    TempData["Error"] = $"Cannot change status from {order.OrderStatus} to {status}";
                    return RedirectToAction("Notifications");
                }
                break;
            default:
                TempData["Error"] = $"Order status {order.OrderStatus} cannot be modified";
                return RedirectToAction("Notifications");
        }

        order.OrderStatus = status;
        _context.SaveChanges();

        TempData["Message"] = $"Order #{orderId} status updated to {status}";
        return RedirectToAction("Notifications");
    }

    // ✅ Messages Page
    public IActionResult Messages()
    {
        var farmerId = GetCurrentFarmerId();
        if (farmerId == null) return RedirectToAction("Login", "Account");

        var messages = _context.Messages
            .Where(m => m.ReceiverID == farmerId.Value)
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        return View(messages);
    }

    // GET: Farmer Profile
    public IActionResult Profile()
    {
        var farmerId = GetCurrentFarmerId();
        if (farmerId == null)
            return RedirectToAction("Login", "Account");

        var farmer = _context.Farmers.Find(farmerId.Value);
        if (farmer == null)
            return NotFound();

        var viewModel = new FarmerProfileViewModel
        {
            UserID = farmer.UserID,
            Name = farmer.Name,
            Email = farmer.Email,
            Phone = farmer.Phone,
            Address = farmer.Address
        };

        return View(viewModel); // This is correct, ensure that the Profile view expects FarmerProfileViewModel
    }


    // POST: Update Farmer Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile(FarmerProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model); // Return the same view if validation fails
        }

        // Fetch the farmer using UserID from the form data
        var farmer = _context.Farmers.Find(model.UserID);
        if (farmer == null)
            return NotFound();

        // Update profile information
        farmer.Name = model.Name;
        farmer.Email = model.Email;
        farmer.Phone = model.Phone;
        farmer.Address = model.Address;

        // Password change section
        if (!string.IsNullOrWhiteSpace(model.CurrentPassword) &&
            !string.IsNullOrWhiteSpace(model.NewPassword) &&
            !string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
        {
            // Validate current password
            if (!VerifyPassword(model.CurrentPassword, farmer.Password))
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
            farmer.Password = HashPassword(model.NewPassword);
        }

        // Save changes to the database
        _context.Update(farmer);
        _context.SaveChanges();

        // Set a success message to display on the UI
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
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

    // ✅ Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // Clear the session
        return RedirectToAction("Index", "Home");
    }
    
}
