using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string Phone { get; set; }
        public string Address { get; set; }

        [Required]
        public string UserType { get; set; } // "Admin", "Farmer", "Buyer"

        public DateTime CreatedAt { get; set; }
        public bool IsEmailVerified { get; set; } // For email verification
       
        public string? OTP { get; set; }  // Note the ? making it nullable
        public DateTime? OTPExpiry { get; set; }
    }

    // Admin class inherits from User
    public class Admin : User
    {
        public string AdminRole { get; set; } // e.g., "SuperAdmin", "SupportAdmin"
    }

    // Farmer class inherits from User
    public class Farmer : User
    {
        public string FarmName { get; set; }
        public string FarmLocation { get; set; }
    }

    // Buyer (Consumer) class inherits from User
    public class Buyer : User
    {
        public string PreferredLocation { get; set; }
    }
}
