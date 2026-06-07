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

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body,
            bool isBodyHtml = false)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new InvalidOperationException(
                    "Email primalac nije postavljen.");
            }

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
                From = CreateFromAddress(),
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml
            };

            message.To.Add(to);

            using var client = new SmtpClient(
                _settings.EffectiveHost,
                _settings.Port)
            {
                EnableSsl = _settings.EnableSsl
            };

            var userName = _settings.EffectiveUserName;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                client.Credentials = new NetworkCredential(
                    userName,
                    _settings.Password);
            }

            await client.SendMailAsync(message);
        }

        private MailAddress CreateFromAddress()
        {
            return string.IsNullOrWhiteSpace(_settings.SenderName)
                ? new MailAddress(_settings.EffectiveFrom)
                : new MailAddress(
                    _settings.EffectiveFrom,
                    _settings.SenderName);
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_settings.EffectiveHost) &&
                _settings.Port > 0 &&
                !string.IsNullOrWhiteSpace(_settings.EffectiveFrom) &&
                !string.IsNullOrWhiteSpace(_settings.EffectiveUserName) &&
                !string.IsNullOrWhiteSpace(_settings.Password);
        }
    }
}
