namespace EmployeeServiceApi.DTOs.EmployeeDto;

public class EmployeeListItemDto
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string Grade { get; set; }
    public string Status { get; set; }
    public List<string> Roles { get; set; } = new();
}