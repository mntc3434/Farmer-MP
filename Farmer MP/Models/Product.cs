using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farmer_MP.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public float Price { get; set; }

        public bool Availability { get; set; } = true;

        [Required]
        public string Category { get; set; }

        // Foreign key to User (Farmer)
        [Required]
        public int UserID { get; set; } // This should exist in the database

        [ForeignKey("UserID")]
        [NotMapped] // ✅ This prevents EF from validating the Farmer property
        public virtual User? Farmer { get; set; }

        public virtual List<ProductImage> Images { get; set; } = new List<ProductImage>();
    }

    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
