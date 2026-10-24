namespace EmployeeServiceApi.Entities;

public class Attachment
{
    public int Id { get; set; }

    public int UploadedByEmployeeId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Category { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}