using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Opportunities;

public class JobApplicationHistoryDto
{
    public int Id { get; set; }
    public int JobApplicationId { get; set; }
    public int ActorEmployeeId { get; set; }
    public ApplicationStatus PreviousStatus { get; set; }
    public ApplicationStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}