namespace EmployeeSelfServiceApi.DTOs.LMS;

public class AssignmentLessonDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public int OrderNumber { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// What a learner sees for one assignment. Questions are withheld until every
/// lesson is complete and NEVER include the correct-option flags.
/// </summary>
public class AssignmentContentDto
{
    public int AssignmentId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public List<AssignmentLessonDto> Lessons { get; set; } = new();
    public bool AssessmentOpen { get; set; }
    public List<AssessmentQuestionResponseDto>? Questions { get; set; }
}
