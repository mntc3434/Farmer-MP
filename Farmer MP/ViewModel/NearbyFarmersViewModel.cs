using Farmer_MP.Models;
using System.Collections.Generic;

namespace Farmer_MP.ViewModels
{
    public class FarmerCoordinates
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class NearbyFarmersViewModel
    {
        public string SearchLocation { get; set; }
        public List<User> Farmers { get; set; } = new List<User>();  // Farmer details
        public Dictionary<int, List<Product>> ProductsByFarmer { get; set; } = new Dictionary<int, List<Product>>();  // Products by farmer
        public List<FarmerCoordinates> FarmersCoordinates { get; set; } = new List<FarmerCoordinates>();  // Geocoded farmer coordinates
    }
}
