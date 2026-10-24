using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.LMS;

public class CourseAssignmentResponseDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int CourseId { get; set; }

    public string CourseTitle { get; set; } = string.Empty;

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public CourseAssignmentStatus Status { get; set; }

    public int AttemptsUsed { get; set; }

    public int AttemptCycle { get; set; }

    public int MaximumAttemptsPerCycle { get; set; }

    public decimal? BestScorePercentage { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}