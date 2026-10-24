namespace EmployeeServiceApi.DTOs.KPIDto;

public class KpiReviewPeriodResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly SubmissionDeadline { get; set; }
    public bool IsOpen { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}