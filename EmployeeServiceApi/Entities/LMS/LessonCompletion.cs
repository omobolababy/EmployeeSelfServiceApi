using EmployeeSelfServiceApi.Entities;

namespace EmployeeServiceApi.Entities;

public class LessonCompletion
{
    public int Id { get; set; }

    public int CourseAssignmentId { get; set; }
    public CourseAssignment CourseAssignment { get; set; } = null!;

    public int CourseLessonId { get; set; }

    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}