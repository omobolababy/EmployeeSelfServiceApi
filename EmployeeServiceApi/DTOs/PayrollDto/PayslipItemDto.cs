using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Payroll;

public class PayslipItemDto
{
    public PayslipItemType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}