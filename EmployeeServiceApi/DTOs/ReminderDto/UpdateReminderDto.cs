namespace EmployeeSelfServiceApi.DTOs.Reminders;

public class UpdateReminderDto
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime ScheduledAtUtc { get; set; }
}