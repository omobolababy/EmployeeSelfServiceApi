namespace EmployeeServiceApi.DTOs.EmployeeDto;

public class CreateEmployeeDto
{
    public string EmployeeNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string Grade { get; set; }
    public string? PhoneNumber { get; set; }
    public int? ReportingManagerId { get; set; }
    public List<string> Roles { get; set; } = new();
}