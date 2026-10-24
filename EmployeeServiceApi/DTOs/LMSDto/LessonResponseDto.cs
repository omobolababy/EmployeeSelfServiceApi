namespace EmployeeSelfServiceApi.DTOs.LMS;

public class LessonResponseDto
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? VideoUrl { get; set; }

    public int OrderNumber { get; set; }

    public bool IsRequired { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}