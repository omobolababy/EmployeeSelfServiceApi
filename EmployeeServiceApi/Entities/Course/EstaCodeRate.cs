namespace EmployeeServiceApi.Entities;

public class EstaCodeRate
{
    public int Id { get; set; }

    // ISO 3166-1 alpha-2 country code, e.g. GB, US or NG.
    public string CountryCode { get; set; } = string.Empty;

    public string Grade { get; set; } = string.Empty;

    public decimal DailyRateUsd { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}