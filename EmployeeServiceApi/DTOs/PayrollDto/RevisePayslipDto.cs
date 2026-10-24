namespace EmployeeSelfServiceApi.DTOs.Payroll;

public class RevisePayslipDto
{
    public decimal BasicSalary { get; set; }

    public List<PayslipItemDto> Items { get; set; } = new();

    public string RevisionReason { get; set; } = string.Empty;
}