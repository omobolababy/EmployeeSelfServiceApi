using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Employees;

public class EmployeeProfileDto
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public int? ReportingManagerId { get; set; }
    public string? ReportingManagerName { get; set; }

    public List<string> Roles { get; set; } = new();

    public EmployeeStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
}