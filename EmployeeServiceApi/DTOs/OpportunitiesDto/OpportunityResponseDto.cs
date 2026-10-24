using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Opportunities;

public class OpportunityResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public OpportunityType Type { get; set; }
    public OpportunityStatus Status { get; set; }

    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }

    public int CreatedByEmployeeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<string> EligibleDepartments { get; set; } = new();
    public List<string> EligibleGrades { get; set; } = new();

    public int ApplicationCount { get; set; }
}