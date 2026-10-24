namespace EmployeeSelfServiceApi.DTOs.KPI;

public class KpiTargetDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal WeightPercentage { get; set; }
}