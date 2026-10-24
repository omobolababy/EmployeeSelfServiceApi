using EmployeeSelfServiceApi.Data;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Utilities;

/// <summary>
/// Adds an in-app notification AND a queued email to the change tracker.
/// Nothing is saved here: the caller's SaveChanges commits the notification
/// in the same transaction as the business change. A failed email later
/// never undoes the business action.
/// </summary>
public static class NotificationHelper
{
    public static void Add(
        ApplicationDbContext context,
        Employee recipient,
        NotificationType type,
        string title,
        string message,
        string? relatedEntityType = null,
        int? relatedEntityId = null,
        string? deduplicationKey = null)
    {
        var now = DateTime.UtcNow;

        context.Notifications.Add(new Notification
        {
            EmployeeId = recipient.Id,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            DeduplicationKey = deduplicationKey,
            CreatedAtUtc = now
        });

        if (!string.IsNullOrWhiteSpace(recipient.Email))
        {
            context.EmailQueueItems.Add(EmailTemplates.ToQueueItem(
                recipient.Email,
                EmailTemplates.Subject(type, title),
                EmailTemplates.Notification(recipient.FirstName, title, message),
                now));
        }
    }
}
