namespace EmployeeServiceApi.Entities.KPI;

public class KpiReviewPeriod
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly SubmissionDeadline { get; set; }

    public bool IsOpen { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<KpiPlan> Plans { get; set; }
        = new List<KpiPlan>();

}