namespace EmployeeServiceApi.Entities;

public class AssessmentAnswer
{
    public int Id { get; set; }

    public int AssessmentAttemptId { get; set; }
    public AssessmentAttempt AssessmentAttempt { get; set; } = null!;

    public int AssessmentQuestionId { get; set; }
    public int? SelectedOptionId { get; set; }

    public bool IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
}