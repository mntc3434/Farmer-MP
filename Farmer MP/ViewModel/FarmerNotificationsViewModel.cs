using System;
using System.Collections.Generic;
using Farmer_MP.Models;

namespace Farmer_MP.ViewModel
{
    public class FarmerNotificationsViewModel
    {
        public int NewNotificationsCount { get; set; }
        public int NewMessagesCount { get; set; }
        public List<NotificationItem> Notifications { get; set; }
    }

    public class NotificationItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public int OrderId { get; set; }
        public string CurrentStatus { get; set; }
        public string BuyerName { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public float TotalPrice { get; set; }
    }
}
