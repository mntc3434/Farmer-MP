using System.ComponentModel.DataAnnotations;

namespace Farmer_MP.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        // Foreign key to Sender (User)
        public int SenderID { get; set; }
        public User Sender { get; set; }

        // Foreign key to Receiver (User)
        public int ReceiverID { get; set; }
        public User Receiver { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; } = false;
    }

}
