namespace EmployeeServiceApi.DTOs;

public class EmployeeCreatedResponse
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } 
    public string FullName { get; set; } 
    public string Email { get; set; } 
    public string Status { get; set; } 

    public string? SetupToken { get; set; }
    public DateTime? SetupTokenExpiresAtUtc { get; set; }
}