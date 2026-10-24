using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Notifications;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationListResponseDto> GetMyNotificationsAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return new NotificationListResponseDto
        {
            Notifications = notifications.Select(Map).ToList(),
            UnreadCount = await GetUnreadCountAsync(employeeId, cancellationToken)
        };
    }

    public async Task<PagedResponseDto<NotificationResponseDto>> GetMyNotificationsPagedAsync(
        int employeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            items.Select(Map),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    public async Task<NotificationResponseDto> MarkNotificationAsReadAsync(
        int notificationId,
        int employeeId,
        MarkNotificationReadDto request,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            throw new KeyNotFoundException("Notification was not found.");
        }

        if (notification.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "You can only update your own notifications.");
        }

        if (request.MarkAsRead && notification.ReadAtUtc is null)
        {
            notification.ReadAtUtc = DateTime.UtcNow;
        }
        else if (!request.MarkAsRead)
        {
            notification.ReadAtUtc = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Map(notification);
    }

    public Task<int> GetUnreadCountAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return _context.Notifications.CountAsync(
            n => n.EmployeeId == employeeId && n.ReadAtUtc == null,
            cancellationToken);
    }

    public async Task<PagedResponseDto<EmailQueueStatusDto>> GetEmailQueueAsync(
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmailQueueItems.AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            items.Select(MapEmail),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    public async Task<EmailQueueStatusDto> GetEmailQueueItemAsync(
        int emailQueueItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.EmailQueueItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == emailQueueItemId, cancellationToken);

        if (item is null)
        {
            throw new KeyNotFoundException("Email queue item was not found.");
        }

        return MapEmail(item);
    }

    private static NotificationResponseDto Map(EmployeeServiceApi.Entities.Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        RelatedEntityType = n.RelatedEntityType,
        RelatedEntityId = n.RelatedEntityId,
        CreatedAtUtc = n.CreatedAtUtc,
        ReadAtUtc = n.ReadAtUtc
    };

    // The body is deliberately never returned (it can contain tokens).
    private static EmailQueueStatusDto MapEmail(EmployeeSelfServiceApi.Entities.EmailQueueItem e) => new()
    {
        Id = e.Id,
        RecipientEmail = e.RecipientEmail,
        Subject = e.Subject,
        Status = e.Status,
        AttemptCount = e.AttemptCount,
        LastError = e.LastError,
        CreatedAtUtc = e.CreatedAtUtc,
        NextAttemptAtUtc = e.NextAttemptAtUtc,
        SentAtUtc = e.SentAtUtc
    };
}
