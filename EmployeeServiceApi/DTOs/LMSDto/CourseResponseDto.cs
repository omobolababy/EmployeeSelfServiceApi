using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.LMS;

public class CourseResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Category { get; set; }

    public CourseStatus Status { get; set; }

    public int CreatedByEmployeeId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int LessonCount { get; set; }

    public int QuestionCount { get; set; }
}