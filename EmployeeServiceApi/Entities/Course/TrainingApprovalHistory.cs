using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Entities;

public class TrainingApprovalHistory
{
    public int Id { get; set; }

    public int TrainingRequestId { get; set; }
    public TrainingRequest TrainingRequest { get; set; } = null!;

    public int ActorEmployeeId { get; set; }
    public Employee ActorEmployee { get; set; } = null!;

    public string ActorRole { get; set; } = string.Empty;
    public TrainingDecision Decision { get; set; }

    public TrainingStatus PreviousStatus { get; set; }
    public TrainingStatus NewStatus { get; set; }

    public int? PreviousReviewerId { get; set; }
    public int? NewReviewerId { get; set; }

    public string? Comment { get; set; }
    public string? Reason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}