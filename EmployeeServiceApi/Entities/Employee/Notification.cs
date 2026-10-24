using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities;

public class Notification
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Title { get; set; } 
    public string Message { get; set; } 

    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public string? DeduplicationKey { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}