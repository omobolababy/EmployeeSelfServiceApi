namespace EmployeeServiceApi.Entities.KPI;

public class KpiTarget
{
    public int Id { get; set; }

    public int KpiPlanId { get; set; }
    public KpiPlan KpiPlan { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal WeightPercentage { get; set; }

    public decimal? ActualValue { get; set; }
    public decimal? ScorePercentage { get; set; }
    public string? EmployeeComment { get; set; }
    public string? ReviewerComment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}