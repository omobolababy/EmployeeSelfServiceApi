namespace EmployeeSelfServiceApi.DTOs.Reminders;

public class CreateReminderDto
{
    public int EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public DateTime ScheduledAtUtc { get; set; }

    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public string DeduplicationKey { get; set; } = string.Empty;
}