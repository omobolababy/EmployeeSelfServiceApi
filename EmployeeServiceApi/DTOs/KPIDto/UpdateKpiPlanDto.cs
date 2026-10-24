using EmployeeSelfServiceApi.DTOs.KPI;

namespace EmployeeServiceApi.DTOs.KPI;

public class UpdateKpiPlanDto
{
    public List<KpiTargetDto> Targets { get; set; } = new();
}