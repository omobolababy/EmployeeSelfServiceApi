using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.Payslip;

public class PayslipVersion
{
    public int Id { get; set; }

    public int PayslipId { get; set; }
    public Payslip Payslip { get; set; } = null!;

    public int VersionNumber { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public string Currency { get; set; } = "NGN";
    public PayslipStatus Status { get; set; } = PayslipStatus.Draft;

    public int PreparedByEmployeeId { get; set; }
    public int? PublishedByEmployeeId { get; set; }

    public string? RevisionReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<PayslipItem> Items { get; set; }
        = new List<PayslipItem>();
}