using EmployeeSelfServiceApi.Entities;

namespace EmployeeServiceApi.Entities;

public class AssessmentQuestion
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public decimal Marks { get; set; } = 1m;

    public ICollection<AssessmentOption> Options { get; set; } = new List<AssessmentOption>();
}