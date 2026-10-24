using EmployeeSelfServiceApi.DTOs.Notifications;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IAttachmentService _attachmentService;

    public NotificationsController(
        INotificationService notificationService,
        IAttachmentService attachmentService)
    {
        _notificationService = notificationService;
        _attachmentService = attachmentService;
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetMine(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _notificationService.GetMyNotificationsPagedAsync(
            CurrentEmployeeId, pagination, ct));

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
        => Ok(new { unreadCount = await _notificationService.GetUnreadCountAsync(CurrentEmployeeId, ct) });

    [HttpPut("notifications/{notificationId:int}/read")]
    public async Task<IActionResult> MarkRead(
        int notificationId, CancellationToken ct)
        => Ok(await _notificationService.MarkNotificationAsReadAsync(
            notificationId, CurrentEmployeeId, new MarkNotificationReadDto { MarkAsRead = true }, ct));

    /// Email queue status (attempts, errors) for the admin to verify delivery
    [Authorize(Roles = "Admin")]
    [HttpGet("email-queue")]
    public async Task<IActionResult> EmailQueue(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _notificationService.GetEmailQueueAsync(pagination, ct));

    [Authorize(Roles = "Admin")]
    [HttpGet("email-queue/{emailQueueItemId:int}")]
    public async Task<IActionResult> EmailQueueItem(int emailQueueItemId, CancellationToken ct)
        => Ok(await _notificationService.GetEmailQueueItemAsync(emailQueueItemId, ct));

    /// Private files download only here, authorised against the parent record
    [HttpGet("attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> Download(int attachmentId, CancellationToken ct)
    {
        var file = await _attachmentService.GetDownloadAsync(attachmentId, CurrentEmployeeId, ct);

        return PhysicalFile(file.PhysicalPath, file.ContentType, file.DownloadFileName);
    }
}
