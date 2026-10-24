namespace EmployeeSelfServiceApi.DTOs.KPI;

public class KpiTargetResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal WeightPercentage { get; set; }

    public decimal? ActualValue { get; set; }

    /// <summary>Achievement % = min(actual / target x 100, 100).</summary>
    public decimal? ScorePercentage { get; set; }
    /// <summary>Achievement / 100 x weight.</summary>
    public decimal? WeightedScore { get; set; }

    public string? EmployeeComment { get; set; }

    public string? ReviewerComment { get; set; }
}