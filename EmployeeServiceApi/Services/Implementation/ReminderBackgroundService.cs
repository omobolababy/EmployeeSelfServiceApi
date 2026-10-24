using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.Entities;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
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
                    "Error while processing automated reminders.");
            }

            // Runs hourly. The deduplication key (recipient, item, type,
            // Africa/Lagos date) guarantees at most one reminder per day.
            await Task.Delay(
                TimeSpan.FromHours(1),
                stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        var today = ReminderUtilities.GetLagosDate(now);

        await ProcessCourseRemindersAsync(
            context,
            today,
            cancellationToken);

        await ProcessKpiRemindersAsync(
            context,
            today,
            cancellationToken);
    }

    private async Task ProcessCourseRemindersAsync(
        ApplicationDbContext context,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var tomorrow = today.AddDays(1);
        var tomorrowRange =
            ReminderUtilities.GetUtcRangeForLagosDate(tomorrow);

        var assignments = await context.CourseAssignments
            .Include(a => a.Employee)
            .Where(a =>
                a.DueAtUtc != null &&
                a.DueAtUtc <= tomorrowRange.EndUtc &&
                a.Status != CourseAssignmentStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var dueDate = ReminderUtilities.GetLagosDate(
                assignment.DueAtUtc!.Value);

            // Only remind about courses due tomorrow or already overdue.
            if (dueDate > tomorrow)
                continue;

            var reminderType = dueDate == tomorrow
                ? "CourseDueTomorrow"
                : "CourseOverdue";

            var message = dueDate == tomorrow
                ? $"Your course assignment {assignment.CourseId} is due tomorrow."
                : $"Your course assignment {assignment.CourseId} is overdue.";

            await QueueReminderAsync(
                context,
                assignment.EmployeeId,
                assignment.Employee.Email,
                assignment.Employee.FirstName,
                "CourseAssignment",
                assignment.Id,
                reminderType,
                today,
                "Course reminder",
                message,
                cancellationToken);
        }
    }

    private async Task ProcessKpiRemindersAsync(
        ApplicationDbContext context,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var plans = await context.KpiPlans
            .Include(p => p.Employee)
            .Include(p => p.AssignedReviewer)
            .ToListAsync(cancellationToken);

        foreach (var plan in plans)
        {
            if (plan.Status != KpiPlanStatus.Active &&
                plan.Status != KpiPlanStatus.Returned)
                continue;

            var deadline = plan.RevisedDeadline
                ?? plan.SubmissionDeadline;

            string reminderType;

            if (deadline == today.AddDays(3))
            {
                reminderType = "KpiThreeDayReminder";
            }
            else if (deadline == today)
            {
                reminderType = "KpiDeadlineDay";
            }
            else
            {
                continue;
            }

            var message =
                $"KPI plan {plan.Id} has a submission deadline of {deadline:yyyy-MM-dd}.";

            await QueueReminderAsync(
                context,
                plan.EmployeeId,
                plan.Employee.Email,
                plan.Employee.FirstName,
                "KpiPlan",
                plan.Id,
                reminderType,
                today,
                "KPI deadline reminder",
                message,
                cancellationToken);

            if (plan.AssignedReviewer is not null &&
                plan.AssignedReviewerId != plan.EmployeeId)
            {
                await QueueReminderAsync(
                    context,
                    plan.AssignedReviewer.Id,
                    plan.AssignedReviewer.Email,
                    plan.AssignedReviewer.FirstName,
                    "KpiPlan",
                    plan.Id,
                    reminderType,
                    today,
                    "KPI review reminder",
                    message,
                    cancellationToken);
            }
        }
    }

    private async Task QueueReminderAsync(
        ApplicationDbContext context,
        int recipientEmployeeId,
        string? recipientEmail,
        string? recipientName,
        string itemType,
        int itemId,
        string reminderType,
        DateOnly reminderDate,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var key = ReminderUtilities.BuildDeduplicationKey(
            recipientEmployeeId,
            itemType,
            itemId,
            reminderType,
            reminderDate);

        var exists = await context.Notifications
            .AnyAsync(
                n => n.DeduplicationKey == key,
                cancellationToken);

        if (exists)
            return;

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            // Recheck after acquiring the transaction.
            exists = await context.Notifications
                .AnyAsync(
                    n => n.DeduplicationKey == key,
                    cancellationToken);

            if (exists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            var now = DateTime.UtcNow;

            context.Notifications.Add(new Notification
            {
                EmployeeId = recipientEmployeeId,
                Type = NotificationType.Reminder,
                Title = title,
                Message = message,
                RelatedEntityType = itemType,
                RelatedEntityId = itemId,
                DeduplicationKey = key,
                CreatedAtUtc = now
            });

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                context.EmailQueueItems.Add(new EmailQueueItem
                {
                    RecipientEmail = recipientEmail,
                    Subject = EmailTemplates.Subject(
                        NotificationType.Reminder, title),
                    Body = EmailTemplates.Reminder(
                        recipientName ?? "there", title, message),
                    Status = EmailMessageStatus.Pending,
                    AttemptCount = 0,
                    CreatedAtUtc = now,
                    NextAttemptAtUtc = now
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}