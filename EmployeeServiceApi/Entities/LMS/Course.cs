using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.Entities;

public class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    public int CreatedByEmployeeId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<CourseLesson> Lessons { get; set; }
        = new List<CourseLesson>();

    public ICollection<AssessmentQuestion> Questions { get; set; }
        = new List<AssessmentQuestion>();

    public ICollection<CourseAssignment> Assignments { get; set; }
        = new List<CourseAssignment>();
}