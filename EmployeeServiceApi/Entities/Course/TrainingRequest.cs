using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.LMS;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Entities;

public class TrainingRequest
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public DateOnly TrainingStartDate { get; set; }
    public DateOnly TrainingEndDate { get; set; }
    public DateOnly? DepartureDate { get; set; }
    public DateOnly? ReturnDate { get; set; }

    public string BusinessJustification { get; set; } = string.Empty;

    public decimal CourseFee { get; set; }
    public string CourseFeeCurrency { get; set; } = "NGN";

    public decimal FlightEstimate { get; set; }
    public string FlightCurrency { get; set; } = "NGN";

    public TrainingStatus Status { get; set; } =
        TrainingStatus.Draft;

    public int? AssignedReviewerId { get; set; }
    public Employee? AssignedReviewer { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? WithdrawnAtUtc { get; set; }
    public int? WithdrawnByEmployeeId { get; set; }
    public string? WithdrawalReason { get; set; }

    public ICollection<TrainingApprovalHistory> ApprovalHistory
    { get; set; } = new List<TrainingApprovalHistory>();

    public ICollection<TrainingAllowanceVersion> AllowanceVersions
    { get; set; } = new List<TrainingAllowanceVersion>();

    public ICollection<TrainingCompletionEvidence> CompletionEvidence
    { get; set; } = new List<TrainingCompletionEvidence>();
}