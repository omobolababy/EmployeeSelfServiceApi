namespace EmployeeSelfServiceApi.DTOs.LMS;

public class UpdateLessonDto
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? VideoUrl { get; set; }

    public int OrderNumber { get; set; }

    public bool IsRequired { get; set; } = true;
}