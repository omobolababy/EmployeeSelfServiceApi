using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.JobApplication;

public class JobApplicationHistory
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int ActorEmployeeId { get; set; }

    public ApplicationStatus PreviousStatus { get; set; }
    public ApplicationStatus NewStatus { get; set; }

    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}