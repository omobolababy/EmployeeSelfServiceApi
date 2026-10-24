using EmployeeSelfServiceApi.DTOs.Notifications;
using EmployeeSelfServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponseDto> GetMyNotificationsAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<NotificationResponseDto>> GetMyNotificationsPagedAsync(
        int employeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<NotificationResponseDto> MarkNotificationAsReadAsync(
        int notificationId,
        int employeeId,
        MarkNotificationReadDto request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<EmailQueueStatusDto>> GetEmailQueueAsync(
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<EmailQueueStatusDto> GetEmailQueueItemAsync(
        int emailQueueItemId,
        CancellationToken cancellationToken = default);
}