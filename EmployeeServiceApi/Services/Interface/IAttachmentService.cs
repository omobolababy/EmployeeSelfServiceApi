using Microsoft.AspNetCore.Http;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public class AttachmentResponseDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public record AttachmentDownload(
    string PhysicalPath,
    string ContentType,
    string DownloadFileName);

public interface IAttachmentService
{
    Task<AttachmentResponseDto> UploadAsync(
        string entityType,
        int entityId,
        string category,
        IFormFile file,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);

    Task<AttachmentDownload> GetDownloadAsync(
        int attachmentId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default);
}
