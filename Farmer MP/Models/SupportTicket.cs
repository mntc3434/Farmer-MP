using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class SupportTicket
    {
        [Key]
        public int TicketID { get; set; }

        // Foreign key to User (who raised the ticket)
        public int UserID { get; set; }
        public User User { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Status { get; set; } // e.g., "Open", "In Progress", "Resolved"

        public DateTime CreatedAt { get; set; }
    }
}
