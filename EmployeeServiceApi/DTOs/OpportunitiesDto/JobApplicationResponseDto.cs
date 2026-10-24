using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Opportunities;

public class JobApplicationResponseDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    public int OpportunityId { get; set; }
    public string OpportunityTitle { get; set; } = string.Empty;
    public OpportunityType OpportunityType { get; set; }

    public ApplicationStatus Status { get; set; }
    public string? CoverLetter { get; set; }
    public int? AttachmentId { get; set; }

    public string DepartmentSnapshot { get; set; } = string.Empty;
    public string GradeSnapshot { get; set; } = string.Empty;
    public bool EligibleAtSubmission { get; set; }

    public DateTime AppliedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? WithdrawnAtUtc { get; set; }
}