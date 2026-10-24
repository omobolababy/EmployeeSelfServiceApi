using EmployeeSelfServiceApi.DTOs.Payroll;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IPayslipPdfService
{
    byte[] Generate(
        PayslipResponseDto payslip,
        PayslipVersionResponseDto version);

}