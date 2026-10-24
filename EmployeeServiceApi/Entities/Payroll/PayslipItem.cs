using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.Payslip;

public class PayslipItem
{
    public int Id { get; set; }

    public int PayslipVersionId { get; set; }
    public PayslipVersion PayslipVersion { get; set; } = null!;

    public PayslipItemType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}