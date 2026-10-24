using System.Net;
using EmployeeSelfServiceApi.Entities;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Utilities;

/// <summary>
/// All outgoing email content lives here (same "Utils" approach as the
/// Ride-Hailing API): one branded HTML layout, plus a method per message.
/// Every dynamic value is HTML-encoded before it is placed in the markup.
/// </summary>
public static class EmailTemplates
{
    private const string Brand = "Employee Self-Service";

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>The shared layout every email uses.</summary>
    public static string Layout(string heading, string innerHtml)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /></head>
        <body style="margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f6f8;padding:24px 0;">
            <tr><td align="center">
              <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;width:100%;background-color:#ffffff;border-radius:8px;overflow:hidden;">
                <tr><td style="background-color:#0f4c81;padding:20px 28px;color:#ffffff;font-size:18px;font-weight:bold;">{{Brand}}</td></tr>
                <tr><td style="padding:28px;">
                  <h2 style="margin:0 0 16px 0;font-size:20px;color:#0f4c81;">{{E(heading)}}</h2>
                  {{innerHtml}}
                </td></tr>
                <tr><td style="padding:16px 28px;background-color:#f9fafb;font-size:12px;color:#6b7280;">
                  This is an automated message from {{Brand}}. Please do not reply to this email.
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }

    private static string Paragraph(string text) =>
        $"<p style=\"margin:0 0 14px 0;font-size:15px;line-height:1.5;\">{text}</p>";

    private static string CodeBox(string code) =>
        "<div style=\"margin:18px 0;padding:14px 16px;background-color:#eef4fb;border:1px dashed #0f4c81;" +
        "border-radius:6px;font-family:Consolas,'Courier New',monospace;font-size:15px;" +
        $"word-break:break-all;color:#0f4c81;\"><strong>{E(code)}</strong></div>";

    private static string Button(string url, string label) =>
        $"<p style=\"margin:20px 0;\"><a href=\"{E(url)}\" style=\"background-color:#0f4c81;color:#ffffff;" +
        $"text-decoration:none;padding:12px 22px;border-radius:6px;font-size:15px;display:inline-block;\">{E(label)}</a></p>";

    public static string AccountSetup(string firstName, string token, string? link, int expiryHours)
    {
        var html = Paragraph($"Hello {E(firstName)},") +
                   Paragraph("An employee account has been created for you. Use the setup token below to set your password and activate the account.") +
                   CodeBox(token);

        if (!string.IsNullOrWhiteSpace(link))
        {
            html += Button(link, "Set up my account");
        }

        html += Paragraph($"This token expires in {expiryHours} hours and can only be used once. " +
                          "If you were not expecting this email, please ignore it.");

        return Layout("Welcome to Employee Self-Service", html);
    }

    public static string PasswordReset(string firstName, string token, string? link, int expiryMinutes)
    {
        var html = Paragraph($"Hello {E(firstName)},") +
                   Paragraph("We received a request to reset your password. Use the reset token below.") +
                   CodeBox(token);

        if (!string.IsNullOrWhiteSpace(link))
        {
            html += Button(link, "Reset my password");
        }

        html += Paragraph($"This token expires in {expiryMinutes} minutes and can only be used once. " +
                          "If you did not request a reset, you can safely ignore this email.");

        return Layout("Reset your password", html);
    }

    /// <summary>Generic notification email used by workflow events.</summary>
    public static string Notification(string firstName, string title, string message)
    {
        var greeting = string.IsNullOrWhiteSpace(firstName)
            ? "Hello,"
            : $"Hello {E(firstName)},";

        var html = Paragraph(greeting) +
                   Paragraph(E(message)) +
                   Paragraph("Please sign in to Employee Self-Service to view the details.");

        return Layout(title, html);
    }

    public static string Reminder(string firstName, string title, string message) =>
        Notification(firstName, title, message);

    public static string Subject(NotificationType type, string title) => type switch
    {
        NotificationType.Reminder => $"Reminder: {title}",
        _ => title
    };

    /// <summary>
    /// Adds a queued email to the change tracker so it is saved in the same
    /// transaction as the business change that caused it.
    /// </summary>
    public static EmailQueueItem ToQueueItem(string toEmail, string subject, string htmlBody, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        return new EmailQueueItem
        {
            RecipientEmail = toEmail,
            Subject = subject,
            Body = htmlBody,
            Status = EmailMessageStatus.Pending,
            AttemptCount = 0,
            CreatedAtUtc = now,
            NextAttemptAtUtc = now
        };
    }
}
