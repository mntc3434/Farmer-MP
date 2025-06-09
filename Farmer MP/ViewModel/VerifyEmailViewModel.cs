using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.ViewModel
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "OTP must contain only numbers")]
        public string OTP { get; set; }
    }
}