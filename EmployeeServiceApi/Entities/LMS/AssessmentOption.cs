namespace EmployeeServiceApi.Entities;

public class AssessmentOption
{
    public int Id { get; set; }

    public int AssessmentQuestionId { get; set; }
    public AssessmentQuestion AssessmentQuestion { get; set; } = null!;

    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}