using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Notifications;

public class EmailQueueStatusDto
{
    public int Id { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailMessageStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}