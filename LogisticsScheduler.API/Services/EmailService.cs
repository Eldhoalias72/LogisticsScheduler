using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace LogisticsScheduler.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"];
            _senderEmail = configuration["SendGrid:SenderEmail"];
            _senderName = configuration["SendGrid:SenderName"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent);

            var response = await client.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new Exception($"SendGrid email failed: {body}");
            }
        }
    }
}
