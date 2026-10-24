namespace EmployeeServiceApi.DTOs.Training;

public class CreateEstacodeRateDto
{
    public string CountryCode { get; set; } = string.Empty;

    public string Grade { get; set; } = string.Empty;

    public decimal DailyRateUsd { get; set; }

    public bool IsActive { get; set; } = true;
}