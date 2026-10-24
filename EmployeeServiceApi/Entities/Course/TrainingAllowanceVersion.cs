using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Entities;

public class TrainingAllowanceVersion
{
    public int Id { get; set; }

    public int TrainingRequestId { get; set; }
    public TrainingRequest TrainingRequest { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string CountryCodeSnapshot { get; set; } = string.Empty;
    public string GradeSnapshot { get; set; } = string.Empty;

    public decimal DailyRateUsdSnapshot { get; set; }
    public int EligibleDays { get; set; }
    public decimal TotalAmountUsd { get; set; }

    public AllowanceStatus Status { get; set; } =
        AllowanceStatus.Provisional;

    public bool IsCurrent { get; set; } = true;

    public string? CorrectionReason { get; set; }

    public int CreatedByEmployeeId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? ApprovedByEmployeeId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}