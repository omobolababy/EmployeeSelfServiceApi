using EmployeeSelfServiceApi.DTOs.Dashboard;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IDashboardService
{
    Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<ManagerDashboardDto> GetManagerDashboardAsync(
        int managerId,
        CancellationToken cancellationToken = default);

    Task<HrDashboardDto> GetHrDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<FinanceDashboardDto> GetFinanceDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<AdminDashboardDto> GetAdminDashboardAsync(
        CancellationToken cancellationToken = default);
    
}