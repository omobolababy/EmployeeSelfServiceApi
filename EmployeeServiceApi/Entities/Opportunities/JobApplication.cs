using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.JobApplication;

public class JobApplication
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;

    public ApplicationStatus Status { get; set; } =
        ApplicationStatus.Submitted;

    public string? CoverLetter { get; set; }
    public int? AttachmentId { get; set; }

    // Snapshot of eligibility at the time of application.
    public string DepartmentSnapshot { get; set; } = string.Empty;
    public string GradeSnapshot { get; set; } = string.Empty;
    public bool EligibleAtSubmission { get; set; }

    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? WithdrawnAtUtc { get; set; }

    public ICollection<JobApplicationHistory> History { get; set; }
        = new List<JobApplicationHistory>();
}