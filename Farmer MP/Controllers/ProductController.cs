using Farmer_MP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Farmer_MP.Data;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace Farmer_MP.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Index (View all products)
        public async Task<IActionResult> Index()
        {
            int? userId = GetLoggedInUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); // Or wherever your login page is
            }

            var products = await _context.Products
                .Where(p => p.UserID == userId) // Show only the logged-in user's products
                .Include(p => p.Images)
                .ToListAsync();

            return View(products);
        }

        // GET: Product/Details/5 (View product details)
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            int? userId = GetLoggedInUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect if user is not authenticated
            }

            var product = new Product
            {
                UserID = userId.Value // Assign the logged-in user ID
            };
            return View(product);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile> images)
        {
            int? userId = GetLoggedInUserId();
            if (userId == null)
            {
                return RedirectToAction("Login","Account"); // Redirect if no user is logged in
            }

            if (ModelState.IsValid)
            {
                product.UserID = userId.Value; // Assign correct logged-in user ID
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Save product images if uploaded
                if (images != null && images.Count > 0)
                {
                    var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    foreach (var image in images)
                    {
                        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(image.FileName)}_{Path.GetRandomFileName()}{Path.GetExtension(image.FileName)}";
                        var filePath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductID = product.ProductID,
                            ImageUrl = $"/uploads/{uniqueFileName}"
                        };
                        _context.ProductImages.Add(productImage);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductID == id);

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
                Category = product.Category,
                Images = product.Images.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.ProductID == model.ProductID);

                if (product == null)
                {
                    return NotFound();
                }

                // Update basic product details
                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Availability = model.Availability;
                product.Category = model.Category;

                // Handle image update if new image was provided
                if (model.NewImages != null && model.NewImages.Count > 0)
                {
                    // Get the first image (assuming single image per product)
                    var newImage = model.NewImages.First();

                    // Delete the old image if it exists
                    if (product.Images != null && product.Images.Any())
                    {
                        var oldImage = product.Images.First();
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                            "wwwroot", oldImage.ImageUrl.TrimStart('/'));

                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        _context.ProductImages.Remove(oldImage);
                    }

                    // Save the new image
                    var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(newImage.FileName)}";
                    var filePath = Path.Combine("wwwroot/uploads", uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await newImage.CopyToAsync(stream);
                    }

                    // Add the new image
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = $"/uploads/{uniqueFileName}"
                    });
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If we got here, something failed; redisplay form
            return View(model);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            bool isProductInOrders = await _context.Orders.AnyAsync(o => o.ProductID == id);

            if (isProductInOrders)
            {
                // Optionally add a message to inform user why the delete failed
                ModelState.AddModelError(string.Empty, "Cannot delete this product because it is associated with existing orders.");
                return View(product); // Or redirect with an error message
            }

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Get logged-in user's ID from session
        private int? GetLoggedInUserId()
        {
            return HttpContext.Session.GetInt32("UserID");
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
        // GET: Product/GetByFarmer/5
        [HttpGet]
        public async Task<IActionResult> GetByFarmer(int farmerId)
        {
            var products = await _context.Products
                .Where(p => p.UserID == farmerId)
                .Include(p => p.Images)
                .ToListAsync();

            if (products == null || products.Count == 0)
            {
                return Json(new { success = false, message = "No products found for this farmer." });
            }

            // Optionally shape data to avoid sending everything
            var result = products.Select(p => new
            {
                p.ProductID,
                p.Name,
                p.Description,
                p.Price,
                Images = p.Images.Select(img => img.ImageUrl)
            });

            return Json(new { success = true, products = result });
        }

    }
}
