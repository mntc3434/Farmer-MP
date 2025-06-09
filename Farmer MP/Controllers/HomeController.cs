using Microsoft.AspNetCore.Mvc;
using Farmer_MP.Models;
using Microsoft.EntityFrameworkCore;
using Farmer_MP.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Farmer_MP.ViewModels;
using Newtonsoft.Json;

namespace Farmer_MP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ProductList()
        {
            var products = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Farmer)
                .ToList();

            return View(products);
        }
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Farmer)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Get reviews for the product
            var reviews = _context.Reviews
                .Include(r => r.Buyer)
                .Where(r => r.ProductID == id)
                .OrderByDescending(r => r.Timestamp)
                .ToList();

            ViewBag.Reviews = reviews;

            return View(product);
        }



        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Cart()
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect if not logged in
            }

            var cartItems = _context.ShoppingCarts
                                    .Include(c => c.Product)
                                        .ThenInclude(p => p.Images) // Load product images
                                    .Include(c => c.Product)
                                        .ThenInclude(p => p.Farmer) // Load farmer info
                                    .Include(c => c.Buyer)
                                    .Where(c => c.IsActive && c.BuyerID == buyerId) // Filter by logged-in user
                                    .ToList();

            if (cartItems == null)
            {
                cartItems = new List<ShoppingCart>();
            }

            ViewBag.Products = cartItems.Select(c => c.Product).Where(p => p != null).ToList();

            return View(cartItems);
        }

        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return Json(new { success = false, message = "Please log in to add items to the cart." });
            }

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            var cartItem = _context.ShoppingCarts
                .FirstOrDefault(c => c.ProductID == productId && c.BuyerID == buyerId && c.IsActive);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new ShoppingCart
                {
                    ProductID = productId,
                    BuyerID = buyerId.Value,
                    Quantity = quantity,
                    IsActive = true
                };
                _context.ShoppingCarts.Add(cartItem);
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "Item added to cart!" });
        }


        [HttpPost]
        public IActionResult RemoveFromCart(int cartId)
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return Unauthorized();
            }

            var cartItem = _context.ShoppingCarts.FirstOrDefault(c => c.CartID == cartId && c.BuyerID == buyerId);
            if (cartItem != null)
            {
                _context.ShoppingCarts.Remove(cartItem);
                _context.SaveChanges();
            }
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult UpdateCartQuantity(int cartId, int quantity)
        {
            int? buyerId = HttpContext.Session.GetInt32("UserID");

            if (buyerId == null)
            {
                return Unauthorized();
            }

            var cartItem = _context.ShoppingCarts.FirstOrDefault(c => c.CartID == cartId && c.BuyerID == buyerId);
            if (cartItem != null && quantity > 0)
            {
                cartItem.Quantity = quantity;
                _context.SaveChanges();
            }
            return RedirectToAction("Cart");
        }
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return Json(new { success = false, message = "User not logged in." });
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserID == userId && f.ProductID == productId);

            if (existingFavorite != null)
            {
                // Remove from favorites
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false, message = "Removed from favorites." });
            }
            else
            {
                // Add to favorites
                var favorite = new Favorite
                {
                    UserID = userId.Value,
                    ProductID = productId
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true, message = "Added to favorites." });
            }
        }
        public async Task<IActionResult> NearbyFarmersWithProducts(string searchLocation)
        {
            var viewModel = new NearbyFarmersViewModel();

            if (!string.IsNullOrEmpty(searchLocation))
            {
                // Fetch farmers based on search location
                var farmers = await _context.Users
                    .Where(u => u.UserType == "Farmer" && u.Address.Contains(searchLocation))
                    .ToListAsync();

                viewModel.Farmers = farmers;

                // Fetch products by each farmer
                foreach (var farmer in farmers)
                {
                    var products = await _context.Products
                        .Include(p => p.Images)
                        .Where(p => p.UserID == farmer.UserID)
                        .ToListAsync();

                    viewModel.ProductsByFarmer.Add(farmer.UserID, products);
                }

                // Create a list to hold the geocoded data for farmer coordinates
                var farmersCoordinates = new List<FarmerCoordinates>();

                foreach (var farmer in farmers)
                {
                    var address = farmer.Address;
                    var geocodeResult = await GetGeocodeData(address);

                    if (geocodeResult != null)
                    {
                        var coordinates = new FarmerCoordinates
                        {
                            UserID = farmer.UserID,
                            Name = farmer.Name,
                            Address = farmer.Address,
                            Lat = geocodeResult.Lat,
                            Lng = geocodeResult.Lng
                        };

                        farmersCoordinates.Add(coordinates);
                    }
                }

                viewModel.FarmersCoordinates = farmersCoordinates;
            }

            return View(viewModel);
        }

        public async Task<GeocodeResult> GetGeocodeData(string address)
        {
            var apiKey = "85eddfc8b4aa4ffc80f1bda995a6ec12"; // Replace with your OpenCage API key
            var url = $"https://api.opencagedata.com/geocode/v1/json?q={Uri.EscapeDataString(address)}&key={apiKey}";

            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                var result = JsonConvert.DeserializeObject<OpenCageResponse>(response);

                if (result?.Results != null && result.Results.Any())
                {
                    var geometry = result.Results.First().Geometry;
                    return new GeocodeResult
                    {
                        Lat = geometry.Lat,
                        Lng = geometry.Lng
                    };
                }
            }

            return null;
        }

        // ==== Models used by the controller ====

        public class GeocodeResult
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        public class OpenCageResponse
        {
            [JsonProperty("results")]
            public List<OpenCageResultItem> Results { get; set; }
        }

        public class OpenCageResultItem
        {
            [JsonProperty("geometry")]
            public OpenCageGeometry Geometry { get; set; }
        }

        public class OpenCageGeometry
        {
            [JsonProperty("lat")]
            public double Lat { get; set; }

            [JsonProperty("lng")]
            public double Lng { get; set; }
        }

    }
}
