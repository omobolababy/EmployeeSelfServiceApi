namespace EmployeeSelfServiceApi.DTOs.Reminders;

public class ReminderResponseDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}