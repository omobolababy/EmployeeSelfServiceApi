namespace EmployeeSelfServiceApi.DTOs.LMS;

public class CreateCourseDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? Category { get; set; }
}