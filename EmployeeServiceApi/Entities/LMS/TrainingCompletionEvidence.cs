using EmployeeSelfServiceApi.Entities;
using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities.LMS;

public class TrainingCompletionEvidence
{
    public int Id { get; set; }

    public int TrainingRequestId { get; set; }
    public TrainingRequest TrainingRequest { get; set; } = null!;

    public int AttachmentId { get; set; }
    public Attachment Attachment { get; set; } = null!;

    public TrainingEvidenceStatus Status { get; set; } =
        TrainingEvidenceStatus.PendingReview;

    public int SubmittedByEmployeeId { get; set; }
    public DateTime SubmittedAtUtc { get; set; }

    public int? ReviewedByEmployeeId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewComment { get; set; }
}