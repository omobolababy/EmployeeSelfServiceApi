using EmployeeSelfServiceApi.DTOs.Employees;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.EmployeeDto;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IEmployeeService
{
    Task<PagedResponseDto<EmployeeListItemDto>> GetEmployeesAsync(
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileDto> GetEmployeeByIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileDto> CreateEmployeeAsync(
        CreateEmployeeDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileDto> UpdateEmployeeAsync(
        int employeeId,
        UpdateEmployeeDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task SetEmployeeStatusAsync(
        int employeeId,
        EmployeeStatus status,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileDto> UpdateMyPhoneNumberAsync(
        int employeeId,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

    Task<PagedResponseDto<EmployeeListItemDto>> GetMyTeamAsync(
        int managerId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileDto> AssignRolesAsync(
        int employeeId,
        List<string> roleNames,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task ResendSetupLinkAsync(
        int employeeId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
}
