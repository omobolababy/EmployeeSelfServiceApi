namespace EmployeeSelfServiceApi.DTOs.Notifications;

public class NotificationListResponseDto
{
    public List<NotificationResponseDto> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}