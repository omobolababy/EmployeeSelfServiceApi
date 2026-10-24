namespace EmployeeServiceApi.Entities;

public class EmployeeSetupToken
{
    public int Id { get; set; }
    
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; }
    
    public string TokenHash { get; set; }
    
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}