namespace EmployeeServiceApi.Entities.KPI;

public class KpiReopeningHistory
{
    public int Id { get; set; }

    public int KpiPlanId { get; set; }
    public KpiPlan KpiPlan { get; set; } = null!;

    public int ReopenedByEmployeeId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public DateOnly PreviousDeadline { get; set; }
    public DateOnly NewDeadline { get; set; }

    public DateTime ReopenedAtUtc { get; set; } = DateTime.UtcNow;
}