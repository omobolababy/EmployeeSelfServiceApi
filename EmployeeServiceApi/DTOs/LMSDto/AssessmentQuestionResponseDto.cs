namespace EmployeeSelfServiceApi.DTOs.LMS;

public class AssessmentQuestionResponseDto
{
    public int Id { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public decimal Marks { get; set; }

    public List<AssessmentOptionResponseDto> Options { get; set; } = new();
}

public class AssessmentOptionResponseDto
{
    public int Id { get; set; }

    public string OptionText { get; set; } = string.Empty;
}