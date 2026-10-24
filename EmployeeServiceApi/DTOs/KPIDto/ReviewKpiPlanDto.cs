using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.KPI;

public class ReviewKpiPlanDto
{
    public KpiReviewDecision Decision { get; set; }

    /// <summary>Required when returning a plan.</summary>
    public string? ReviewerComment { get; set; }

    /// <summary>Optional per-target feedback. Scores are always calculated by the server.</summary>
    public List<KpiTargetReviewDto> Targets { get; set; } = new();
}

public class KpiTargetReviewDto
{
    public int TargetId { get; set; }
    public string? ReviewerComment { get; set; }
}
