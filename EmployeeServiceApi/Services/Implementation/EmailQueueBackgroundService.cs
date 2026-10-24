using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class EmailQueueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueBackgroundService> _logger;

    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(60)
    };

    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _lockTimeout;
    private readonly int _batchSize;

    public EmailQueueBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EmailQueueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Defaults follow the user stories: every two minutes, 20 per batch.
        _pollInterval = TimeSpan.FromSeconds(Math.Max(5,
            configuration.GetValue("EmailWorker:PollIntervalSeconds", 120)));
        _batchSize = Math.Clamp(
            configuration.GetValue("EmailWorker:BatchSize", 20), 1, 200);
        _lockTimeout = TimeSpan.FromMinutes(Math.Max(1,
            configuration.GetValue("EmailWorker:LockTimeoutMinutes", 15)));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email queue worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimedEmails = await ClaimEmailsAsync(stoppingToken);

                foreach (var email in claimedEmails)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    await ProcessEmailAsync(
                        email.Id,
                        email.LockId,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while processing the email queue.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Email queue worker stopped.");
    }

    private async Task<List<ClaimedEmail>> ClaimEmailsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var staleLockTime = now.Subtract(_lockTimeout);

        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        // Status is stored as text (HasConversion<string>), so it must be
        // compared with the enum NAME, not its numeric value.
        var pendingStatus = EmailMessageStatus.Pending.ToString();

        var emails = await context.EmailQueueItems
            .FromSqlInterpolated($"""
                SELECT TOP ({_batchSize}) *
                FROM [EmailQueueItems] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE [Status] = {pendingStatus}
                  AND [NextAttemptAtUtc] <= {now}
                  AND (
                      [LockedAtUtc] IS NULL
                      OR [LockedAtUtc] < {staleLockTime}
                  )
                ORDER BY [NextAttemptAtUtc], [CreatedAtUtc]
                """)
            .ToListAsync(cancellationToken);

        var claimed = new List<ClaimedEmail>();

        foreach (var email in emails)
        {
            var lockId = Guid.NewGuid().ToString("N");

            email.LockedAtUtc = now;
            email.LockId = lockId;

            claimed.Add(new ClaimedEmail(email.Id, lockId));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return claimed;
    }

    private async Task ProcessEmailAsync(
        int emailId,
        string lockId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var emailService = scope.ServiceProvider
            .GetRequiredService<IEmailService>();

        var email = await context.EmailQueueItems
            .FirstOrDefaultAsync(
                e => e.Id == emailId && e.LockId == lockId,
                cancellationToken);

        if (email == null)
        {
            return;
        }

        try
        {
            await emailService.SendEmailAsync(
                email.RecipientEmail,
                email.Subject,
                email.Body,
                cancellationToken);

            email.Status = EmailMessageStatus.Sent;
            email.SentAtUtc = DateTime.UtcNow;

            // Remove the stored message body after successful delivery.
            email.Body = string.Empty;

            _logger.LogInformation(
                "Queued email {EmailId} was sent successfully.",
                emailId);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Leave the item recoverable when its lock becomes stale.
            throw;
        }
        catch (Exception exception)
        {
            email.AttemptCount++;

            if (email.AttemptCount <= RetryDelays.Length)
            {
                var delay = RetryDelays[email.AttemptCount - 1];

                email.Status = EmailMessageStatus.Pending;
                email.NextAttemptAtUtc = DateTime.UtcNow.Add(delay);
                email.LastError = exception.Message;

                _logger.LogWarning(
                    exception,
                    "Email {EmailId} failed. Retry {RetryNumber} scheduled after {DelayMinutes} minutes.",
                    emailId,
                    email.AttemptCount,
                    delay.TotalMinutes);
            }
            else
            {
                email.Status = EmailMessageStatus.Failed;
                email.LastError = exception.Message;

                _logger.LogError(
                    exception,
                    "Email {EmailId} failed after all retry attempts.",
                    emailId);
            }
        }

        email.LockedAtUtc = null;
        email.LockId = null;

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record ClaimedEmail(int Id, string LockId);
}