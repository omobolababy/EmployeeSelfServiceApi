namespace EmployeeSelfServiceApi.DTOs.LMS;

public class CreateQuestionDto
{
    public string QuestionText { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public decimal Marks { get; set; } = 1;

    public List<CreateAssessmentOptionDto> Options { get; set; } = new();
}

public class CreateAssessmentOptionDto
{
    public string OptionText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}