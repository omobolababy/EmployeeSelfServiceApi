using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.KPI;

public class KpiReviewHistory
{
    public int Id { get; set; }

    public int KpiPlanId { get; set; }
    public KpiPlan KpiPlan { get; set; } = null!;

    public int ActorEmployeeId { get; set; }
    public string Action { get; set; } = string.Empty;

    public string? Comment { get; set; }
    public KpiPlanStatus PreviousStatus { get; set; }
    public KpiPlanStatus NewStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}