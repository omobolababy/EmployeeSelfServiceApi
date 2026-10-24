namespace EmployeeSelfServiceApi.DTOs.LMS;

public class SubmitAssessmentDto
{
    public List<AssessmentAnswerDto> Answers { get; set; } = new();
}

public class AssessmentAnswerDto
{
    public int QuestionId { get; set; }

    public int SelectedOptionId { get; set; }
}