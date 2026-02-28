using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Classified.Shared.Infrastructure.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmail(string receptor, string subject, string body)
        {
            try
            {

                var email = _config["EmailService:Email"];
                var password = _config["EmailService:Password"];
                var host = _config["EmailService:Host"];
                var port = int.Parse(_config["EmailService:Port"]!);

                var smtpClient = new SmtpClient(host, port);

                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;

                smtpClient.Credentials = new NetworkCredential(email, password);

                var message = new MailMessage(email!, receptor, subject, body);
                await smtpClient.SendMailAsync(message);

            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException("Ошибка при отправке email", ex);
            }
        }
    }
}