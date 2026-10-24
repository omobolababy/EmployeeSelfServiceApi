namespace EmployeeSelfServiceApi.DTOs.Training;

public class EstacodeRateResponseDto
{
    public int Id { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public string Grade { get; set; } = string.Empty;

    public decimal DailyRateUsd { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}