using EmployeeServiceApi.DTOs.KPI;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.KPI;

public class KpiPlanResponseDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int ReviewPeriodId { get; set; }

    public string ReviewPeriodName { get; set; } = string.Empty;

    public int? AssignedReviewerId { get; set; }

    public string? AssignedReviewerName { get; set; }

    public KpiPlanStatus Status { get; set; }

    public DateOnly SubmissionDeadline { get; set; }

    public DateOnly? RevisedDeadline { get; set; }

    public decimal? FinalScorePercentage { get; set; }

    public string? ReviewerComment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateOnly EffectiveDeadline { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime? FinalizedAtUtc { get; set; }

    public List<KpiTargetResponseDto> Targets { get; set; } = new();
}