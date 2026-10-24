using EmployeeSelfServiceApi.Entities;

namespace EmployeeServiceApi.Entities;

public class AssignmentResetHistory
{
    
    public int Id { get; set; }

    public int CourseAssignmentId { get; set; }
    public CourseAssignment CourseAssignment { get; set; } = null!;

    public int ResetByEmployeeId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public int PreviousAttemptCycle { get; set; }
    public int NewAttemptCycle { get; set; }

    public DateTime ResetAtUtc { get; set; } = DateTime.UtcNow;
}