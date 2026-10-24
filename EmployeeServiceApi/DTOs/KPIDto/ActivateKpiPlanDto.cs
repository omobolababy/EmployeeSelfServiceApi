namespace EmployeeSelfServiceApi.DTOs.KPI;

public class ActivateKpiPlanDto
{
    /// <summary>
    /// Optional. When omitted, the employee's reporting manager at the moment
    /// of activation becomes the assigned reviewer.
    /// </summary>
    public int? AssignedReviewerId { get; set; }
}
