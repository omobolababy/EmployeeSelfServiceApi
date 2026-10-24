namespace EmployeeSelfServiceApi.DTOs.KPI;

public class UpdateKpiProgressDto
{
    public List<KpiProgressItemDto> Targets { get; set; } = new();
}

public class KpiProgressItemDto
{
    public int TargetId { get; set; }
    public decimal ActualValue { get; set; }
    public string? EvidenceComment { get; set; }
}
