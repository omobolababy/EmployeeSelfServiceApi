namespace EmployeeSelfServiceApi.DTOs.KPI;

public class CreateReviewPeriodDto
{
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateOnly SubmissionDeadline { get; set; }

    public bool IsOpen { get; set; } = true;
}