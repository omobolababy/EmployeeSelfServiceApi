using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Training;

public class TrainingDecisionDto
{
    public TrainingDecision Decision { get; set; }

    public string? Comment { get; set; }

    public string? Reason { get; set; }

    public int? ReassignReviewerId { get; set; }
}