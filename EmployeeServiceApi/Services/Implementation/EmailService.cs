using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EmployeeSelfServiceApi.Services;

public class EmailService : IEmailService
{
    private readonly SmtpMail _smtpMail;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<SmtpMail> smtpMail,
        ILogger<EmailService> logger)
    {
        _smtpMail = smtpMail.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        // Fail with a clear message (the queue records it in LastError)
        // instead of a confusing SMTP exception.
        if (string.IsNullOrWhiteSpace(_smtpMail.Server) ||
            _smtpMail.Port <= 0 ||
            string.IsNullOrWhiteSpace(_smtpMail.SenderEmail))
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set SmtpMail:Server, Port, " +
                "SenderEmail, Username and Password (use user secrets).");
        }

        try
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _smtpMail.SenderName,
                _smtpMail.SenderEmail));

            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = "Please view this message in an HTML-capable email client."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient
            {
                Timeout = 30_000
            };

            // Port 465 uses implicit TLS; 587/25 use STARTTLS.
            var socketOption = _smtpMail.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : _smtpMail.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.StartTlsWhenAvailable;

            _logger.LogInformation(
                "Connecting to SMTP server {Server}:{Port}.",
                _smtpMail.Server,
                _smtpMail.Port);

            await client.ConnectAsync(
                _smtpMail.Server,
                _smtpMail.Port,
                socketOption,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_smtpMail.Username))
            {
                await client.AuthenticateAsync(
                    _smtpMail.Username,
                    _smtpMail.Password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email accepted by SMTP server for {Recipient}.",
                toEmail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Recipient}.",
                toEmail);

            throw;
        }
    }
}
