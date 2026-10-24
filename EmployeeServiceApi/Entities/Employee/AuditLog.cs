namespace EmployeeServiceApi.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public int? ActorEmployeeId { get; set; }
    public string Action { get; set; } 
    public string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Reason { get; set; }
    public string? DetailsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}