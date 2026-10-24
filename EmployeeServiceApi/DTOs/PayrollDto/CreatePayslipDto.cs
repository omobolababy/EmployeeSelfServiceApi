namespace EmployeeSelfServiceApi.DTOs.Payroll;

public class CreatePayslipDto
{
    public int EmployeeId { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public decimal BasicSalary { get; set; }

    public List<PayslipItemDto> Items { get; set; } = new();
}