using System.ComponentModel.DataAnnotations;
......
namespace Farmer_MP.Models
{
    public class FAQ
    {
        [Key]
        public int FAQID { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }
    }
}
