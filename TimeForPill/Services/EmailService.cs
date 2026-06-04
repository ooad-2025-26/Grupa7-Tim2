using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TimeForPill.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (!IsConfigured())
            {
                _logger.LogWarning(
                    "Email nije poslan jer SMTP servis nije kompletno konfigurisan. To={To}, Subject={Subject}",
                    to,
                    subject);

                throw new InvalidOperationException(
                    "SMTP servis nije kompletno konfigurisan.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(to);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                client.Credentials = new NetworkCredential(
                    _settings.UserName,
                    _settings.Password);
            }

            await client.SendMailAsync(message);
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_settings.Host) &&
                _settings.Port > 0 &&
                !string.IsNullOrWhiteSpace(_settings.From) &&
                !string.IsNullOrWhiteSpace(_settings.UserName) &&
                !string.IsNullOrWhiteSpace(_settings.Password);
        }
    }
}
