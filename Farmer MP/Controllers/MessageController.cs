using Farmer_MP.Data;
using Farmer_MP.Models;
using Farmer_MP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Farmer_MP.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MessageController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Action to display the conversation between a buyer and a farmer
        public IActionResult Index(int buyerId, int farmerId, int productId)
        {
            var currentUserId = _httpContextAccessor.HttpContext.Session.GetInt32("UserID");
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = _context.Users.FirstOrDefault(u => u.UserID == currentUserId.Value);
            if (currentUser == null)
            {
                return NotFound();
            }

            // Get all messages involving the current user
            var userMessages = _context.Messages
                .Where(m => m.SenderID == currentUserId.Value || m.ReceiverID == currentUserId.Value)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.Timestamp)
                .ToList();

            // Get all unique contacts
            var contacts = userMessages
                .Select(m => m.SenderID == currentUserId.Value ? m.Receiver : m.Sender)
                .Where(u => u.UserID != currentUserId.Value)
                .GroupBy(u => u.UserID)
                .Select(g => g.First())
                .ToList();

            // Determine conversation partners based on current user type
            if (currentUser.UserType == "Buyer")
            {
                // For buyers, they can only message farmers
                if (farmerId == 0 && contacts.Any())
                {
                    farmerId = contacts.First().UserID;
                }
                buyerId = currentUserId.Value;
            }
            else if (currentUser.UserType == "Farmer")
            {
                // For farmers, they can only message buyers
                if (buyerId == 0 && contacts.Any())
                {
                    buyerId = contacts.First().UserID;
                }
                farmerId = currentUserId.Value;
            }

            // Get messages between the selected users
            var existingMessages = userMessages
                .Where(m => (m.SenderID == buyerId && m.ReceiverID == farmerId) ||
                            (m.SenderID == farmerId && m.ReceiverID == buyerId))
                .OrderBy(m => m.Timestamp)
                .ToList();

            // Prepare view model
            var viewModel = new MessageViewModel
            {
                Messages = existingMessages,
                NewMessage = new Message
                {
                    SenderID = currentUserId.Value,
                    ReceiverID = currentUser.UserType == "Buyer" ? farmerId : buyerId
                },
                ReceiverOptions = contacts,
                BuyerID = buyerId,
                FarmerID = farmerId,
                ProductID = productId,
                Contacts = contacts.Select(u => new ContactViewModel
                {
                    UserID = u.UserID,
                    UserName = u.Name,
                    UserType = u.UserType,
                    LastMessage = userMessages
                        .Where(m => m.SenderID == u.UserID || m.ReceiverID == u.UserID)
                        .OrderByDescending(m => m.Timestamp)
                        .FirstOrDefault()?.Content
                }).ToList(),
                CurrentUserID = currentUserId.Value,
                CurrentUserType = currentUser.UserType
            };

            return View(viewModel);
        }



        // Action to send a new message
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SendMessage(MessageViewModel model)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(model.NewMessage.Content) || model.NewMessage.ReceiverID == 0)
            {
                ModelState.AddModelError("NewMessage.Content", "Message content and receiver are required.");
                return RedirectToAction("Index", new
                {
                    buyerId = model.BuyerID,
                    farmerId = model.FarmerID,
                    productId = model.ProductID
                });
            }

            // Ensure the sender is the current user
            model.NewMessage.SenderID = userId.Value;
            model.NewMessage.Timestamp = DateTime.Now;

            _context.Messages.Add(model.NewMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new
            {
                buyerId = model.BuyerID,
                farmerId = model.FarmerID,
                productId = model.ProductID
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserID");

            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageID == messageId);

            if (message == null)
                return NotFound();

            if (userId != message.SenderID && userId != message.ReceiverID)
                return Unauthorized();

            int buyerId = message.Sender.UserType == "Buyer" ? message.SenderID :
                          message.Receiver.UserType == "Buyer" ? message.ReceiverID : 0;

            int farmerId = message.Sender.UserType == "Farmer" ? message.SenderID :
                           message.Receiver.UserType == "Farmer" ? message.ReceiverID : 0;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { buyerId, farmerId, productId = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int MessageID, string Content)
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageID == MessageID);

            if (message == null)
                return NotFound();

            if (message.SenderID != userId.Value)
                return Unauthorized();

            // Update the message content
            message.Content = Content;
            await _context.SaveChangesAsync();

            // Safely get buyerId and farmerId
            int buyerId = message.Sender.UserType == "Buyer" ? message.SenderID :
                          message.Receiver.UserType == "Buyer" ? message.ReceiverID : 0;

            int farmerId = message.Sender.UserType == "Farmer" ? message.SenderID :
                           message.Receiver.UserType == "Farmer" ? message.ReceiverID : 0;

            return RedirectToAction("Index", new { buyerId, farmerId, productId = 0 });
        }




    }
}
