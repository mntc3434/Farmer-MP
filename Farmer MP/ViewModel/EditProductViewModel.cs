using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class EditProductViewModel
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public float Price { get; set; }

        public bool Availability { get; set; }

        // Make Category optional if it's not really required
        public string? Category { get; set; }  // Nullable if not required

        // Add property for existing images
        public List<ProductImage>? Images { get; set; }

        // Property for new image uploads
        public List<IFormFile>? NewImages { get; set; }
    }
}