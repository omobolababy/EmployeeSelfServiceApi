using EmployeeSelfServiceApi.DTOs.Payroll;
using EmployeeServiceApi.DTOs.Payroll;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IPayrollService
{
    Task<PayslipResponseDto> CreatePayslipAsync(
        CreatePayslipDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PayslipVersionResponseDto> RevisePayslipAsync(
        int payslipId,
        RevisePayslipDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PayslipVersionResponseDto> PublishPayslipAsync(int payslipId,
        int request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PayslipResponseDto> GetPayslipAsync(
        int payslipId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<List<PayslipResponseDto>> GetMyPayslipsAsync(
        int employeeId,
        CancellationToken cancellationToken = default);
    
    Task<PayslipResponseDto> GetPayslipForFinanceAsync(
        int payslipId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PayslipVersionResponseDto> GetPublishedVersionAsync(
        int payslipId,
        int versionId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<PayslipResponseDto> GetMyPayslipAsync(
        int payslipId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
}