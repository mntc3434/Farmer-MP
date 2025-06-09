namespace Farmer_MP.Controllers
{
    internal class CartItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public float Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
}