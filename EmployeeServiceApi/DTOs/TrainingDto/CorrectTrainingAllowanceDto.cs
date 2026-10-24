namespace EmployeeServiceApi.DTOs.Training;

public class CorrectTrainingAllowanceDto
{
    public string Reason { get; set; } = string.Empty;
    public decimal? DailyRateUsd { get; set; }

    public int? EligibleDays { get; set; }
}