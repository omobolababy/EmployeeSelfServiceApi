using EmployeeServiceApi.DTOs.Payroll;

namespace EmployeeSelfServiceApi.DTOs.Payroll;

public class PayslipResponseDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;
    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public int? CurrentVersionId { get; set; }

    public PayslipVersionResponseDto? CurrentVersion { get; set; }
    
    public List<PayslipVersionResponseDto> Versions { get; set; } = new();
}