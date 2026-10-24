using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Opportunities;

public class CreateOpportunityDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public OpportunityType Type { get; set; }

    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }

    public List<string> EligibleDepartments { get; set; } = new();
    public List<string> EligibleGrades { get; set; } = new();
}