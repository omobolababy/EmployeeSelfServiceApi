namespace EmployeeSelfServiceApi.DTOs.KPI;

public class ReopenKpiPlanDto
{
    public string Reason { get; set; } = string.Empty;

    public DateOnly NewDeadline { get; set; }
}