namespace Farmer_MP.Models
{
    public class ProductViewModel
    {
        // Product ID from the Product model
        public int ProductID { get; set; }

        // Name of the product
        public string Name { get; set; }

        // Description of the product
        public string Description { get; set; }

        // Price of the product
        public float Price { get; set; }

        // Product availability
        public bool Availability { get; set; }

        // Category of the product
        public string Category { get; set; }

        // List of image URLs for the product
        public List<string> ImageUrls { get; set; } = new List<string>();

        // Optional: User (Farmer) name (you can use this to show the farmer who listed the product)
        public string FarmerName { get; set; }
    }
}
