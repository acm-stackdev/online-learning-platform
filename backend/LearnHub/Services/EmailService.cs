using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LearnHub.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string username, string verifyLink);
        Task SendPasswordResetEmailAsync(string toEmail, string username, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task SendVerificationEmailAsync(string toEmail, string username, string verifyLink)
        {
            var subject = "Verify your LearnHub email address";
            var body = $"""
                Hi {username},

                Thanks for signing up for LearnHub. Please verify your email address by clicking the link below:

                {verifyLink}

                This link expires in 24 hours. If you didn't create this account, you can ignore this email.
                """;

            return SendAsync(toEmail, subject, body);
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetLink)
        {
            var subject = "Reset your LearnHub password";
            var body = $"""
                Hi {username},

                We received a request to reset your LearnHub password. Click the link below to choose a new one:

                {resetLink}

                This link expires in 1 hour. If you didn't request a password reset, you can ignore this email.
                """;

            return SendAsync(toEmail, subject, body);
        }

        private async Task SendAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Smtp:FromName"], _config["Smtp:FromEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"] ?? "587"), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Sent email {Subject} to {ToEmail}", subject, toEmail);
        }
    }
}
