using EmployeeSelfServiceApi.Entities;

namespace EmployeeServiceApi.Entities;

public class AssessmentAttempt
{
    public int Id { get; set; }

    public int CourseAssignmentId { get; set; }
    public CourseAssignment CourseAssignment { get; set; } = null!;

    public int AttemptCycle { get; set; }
    public int AttemptNumber { get; set; }

    public decimal Score { get; set; }
    public decimal ScorePercentage { get; set; }
    public bool Passed { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }

    public ICollection<AssessmentAnswer> Answers { get; set; }
        = new List<AssessmentAnswer>();
}