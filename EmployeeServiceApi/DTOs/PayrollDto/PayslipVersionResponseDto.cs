using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Payroll;

public class PayslipVersionResponseDto
{
    public int Id { get; set; }

    public int PayslipId { get; set; }

    public int VersionNumber { get; set; }

    public decimal BasicSalary { get; set; }

    public decimal GrossPay { get; set; }

    public decimal TotalDeductions { get; set; }

    public decimal NetPay { get; set; }

    public string Currency { get; set; } = "NGN";

    public PayslipStatus Status { get; set; }

    public int PreparedByEmployeeId { get; set; }

    public int? PublishedByEmployeeId { get; set; }

    public string? RevisionReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public List<PayslipItemResponseDto> Items { get; set; } = new();
}

public class PayslipItemResponseDto
{
    public int Id { get; set; }

    public PayslipItemType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}