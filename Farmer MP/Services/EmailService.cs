using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Farmer_MP.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            using var smtpClient = new SmtpClient(emailSettings["MailServer"])
            {
                Port = int.Parse(emailSettings["MailPort"]),
                Credentials = new NetworkCredential(
                    emailSettings["Sender"],
                    emailSettings["Password"]),
                EnableSsl = bool.Parse(emailSettings["EnableSsl"]),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["Sender"], emailSettings["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}