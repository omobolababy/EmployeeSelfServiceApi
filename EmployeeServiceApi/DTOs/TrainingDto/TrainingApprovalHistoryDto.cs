using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Training;

public class TrainingApprovalHistoryDto
{
    public int Id { get; set; }

    public int TrainingRequestId { get; set; }

    public int ActorEmployeeId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string ActorRole { get; set; } = string.Empty;

    public TrainingDecision Decision { get; set; }

    public TrainingStatus PreviousStatus { get; set; }

    public TrainingStatus NewStatus { get; set; }

    public int? PreviousReviewerId { get; set; }

    public int? NewReviewerId { get; set; }

    public string? Comment { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}