using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.ViewModel
{
    public class FarmerProfileViewModel
    {
        // User Information
        public int UserID { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        // Optional password change fields
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; }

        // Sidebar-specific data
        public int NewOrders { get; set; } = 0; // Default to 0 if not set
        public int NewMessages { get; set; } = 0;
    }
}
