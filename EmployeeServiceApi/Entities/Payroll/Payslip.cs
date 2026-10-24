namespace EmployeeServiceApi.Entities.Payslip;

public class Payslip
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int PayrollMonth { get; set; }
    public int PayrollYear { get; set; }

    public int? CurrentVersionId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<PayslipVersion> Versions { get; set; }
        = new List<PayslipVersion>();
    
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}