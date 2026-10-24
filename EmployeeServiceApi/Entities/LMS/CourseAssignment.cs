using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Entities;

public class CourseAssignment
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DueAtUtc { get; set; }

    public CourseAssignmentStatus Status { get; set; } =
        CourseAssignmentStatus.NotStarted;

    public int AttemptsUsed { get; set; }
    public int AttemptCycle { get; set; } = 1;
    public int MaximumAttemptsPerCycle { get; set; } = 3;

    public decimal? BestScorePercentage { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<LessonCompletion> LessonCompletions { get; set; }
        = new List<LessonCompletion>();

    public ICollection<AssessmentAttempt> AssessmentAttempts { get; set; }
        = new List<AssessmentAttempt>();

    public ICollection<AssignmentResetHistory> ResetHistory { get; set; }
        = new List<AssignmentResetHistory>();
}