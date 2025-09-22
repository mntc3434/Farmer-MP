using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
.
namespace Farmer_MP.Hubs
{
    public class MessageHub : Hub
    {
        public async Task SendMessage(string receiverId)
        {
            // Tell only the receiver to reload
            await Clients.User(receiverId).SendAsync("ReceiveMessage");
        }
    }
}
