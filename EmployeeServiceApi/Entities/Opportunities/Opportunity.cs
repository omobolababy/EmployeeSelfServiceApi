using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.JobApplication;

public class Opportunity
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;

    public OpportunityType Type { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Draft;

    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }

    public int CreatedByEmployeeId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<OpportunityDepartmentEligibility> EligibleDepartments
    { get; set; } = new List<OpportunityDepartmentEligibility>();

    public ICollection<OpportunityGradeEligibility> EligibleGrades
    { get; set; } = new List<OpportunityGradeEligibility>();

    public ICollection<JobApplication> Applications { get; set; }
        = new List<JobApplication>();
}