using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Notifications;

public class NotificationResponseDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public bool IsRead => ReadAtUtc.HasValue;
}