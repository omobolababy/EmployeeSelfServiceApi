using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Entities;

public class EmailQueueItem
{
    public int Id { get; set; }

    public string RecipientEmail { get; set; } 
    public string Subject { get; set; } 
    public string Body { get; set; } 

    public EmailMessageStatus Status { get; set; }
        = EmailMessageStatus.Pending;

    public int AttemptCount { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }

    public DateTime? LockedAtUtc { get; set; }
    public string? LockId { get; set; }
}