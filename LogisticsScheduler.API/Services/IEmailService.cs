using System.Threading.Tasks;

namespace LogisticsScheduler.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlContent);
    }
}
