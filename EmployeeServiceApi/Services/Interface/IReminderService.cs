using EmployeeSelfServiceApi.DTOs.Reminders;
using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IReminderService
{
    Task<ReminderResponseDto> CreateReminderAsync(
        CreateReminderDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<ReminderResponseDto> UpdateReminderAsync(
        int reminderId,
        UpdateReminderDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task CancelReminderAsync(
        int reminderId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<ReminderResponseDto>> GetRemindersAsync(
        PaginationRequestDto pagination,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
}