using Farmer_MP.Models;
using System.Collections.Generic;

namespace Farmer_MP.ViewModels
{
    public class MessageViewModel
    {
        public List<Message> Messages { get; set; } = new List<Message>();
        public Message NewMessage { get; set; } = new Message();
        public List<User> ReceiverOptions { get; set; } = new List<User>();
        public int BuyerID { get; set; }
        public int FarmerID { get; set; }
        public int ProductID { get; set; }
        public List<ContactViewModel> Contacts { get; set; } = new List<ContactViewModel>();
        
            public int CurrentUserID { get; set; }
            public string CurrentUserType { get; set; }
        
    }

}
