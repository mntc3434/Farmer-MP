using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class EditUserViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid Phone Number")]
        public string Phone { get; set; }

        public string Address { get; set; }
    }
}
