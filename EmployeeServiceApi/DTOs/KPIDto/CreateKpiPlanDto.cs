namespace EmployeeSelfServiceApi.DTOs.KPI;

/// <summary>HR creates a Draft plan for an employee and review period.</summary>
public class CreateKpiPlanDto
{
    public int EmployeeId { get; set; }
    public int ReviewPeriodId { get; set; }
    public List<KpiTargetDto> Targets { get; set; } = new();
}
