
namespace Classified.Shared.Infrastructure.EmailService
{
    public interface IEmailService
    {
        Task SendEmail(string receptor, string subject, string body);
    }
}