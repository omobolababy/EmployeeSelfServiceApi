using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.KPI;

public class KpiPlan
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int ReviewPeriodId { get; set; }
    public KpiReviewPeriod ReviewPeriod { get; set; } = null!;

    // Chosen by HR (or defaulted to the reporting manager) when the plan is activated.
    public int? AssignedReviewerId { get; set; }
    public Employee? AssignedReviewer { get; set; }

    public KpiPlanStatus Status { get; set; } = KpiPlanStatus.Draft;

    public DateOnly SubmissionDeadline { get; set; }
    public DateOnly? RevisedDeadline { get; set; }

    public decimal? FinalScorePercentage { get; set; }
    public string? ReviewerComment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }

    public ICollection<KpiTarget> Targets { get; set; }
        = new List<KpiTarget>();

    public ICollection<KpiReviewHistory> ReviewHistory { get; set; }
        = new List<KpiReviewHistory>();

    public ICollection<KpiReopeningHistory> ReopeningHistory { get; set; }
        = new List<KpiReopeningHistory>();
}