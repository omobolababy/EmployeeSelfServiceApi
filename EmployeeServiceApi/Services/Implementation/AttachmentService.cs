using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

/// <summary>
/// Secure file handling: size/type limits, content sniffing, random stored
/// names, storage outside the web root, and downloads authorised against the
/// parent record.
/// </summary>
public class AttachmentService : IAttachmentService
{
    public const string TrainingRequestType = "TrainingRequest";
    public const string KpiPlanType = "KpiPlan";
    public const string OpportunityType = "Opportunity";

    private static readonly Dictionary<string, string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        };

    // entity type -> categories accepted for upload
    private static readonly Dictionary<string, string[]> AllowedCategories =
        new(StringComparer.Ordinal)
        {
            [TrainingRequestType] = new[] { "SupportingDocument", "CompletionEvidence" },
            [KpiPlanType] = new[] { "KpiEvidence" },
            [OpportunityType] = new[] { "ApplicationCv" }
        };

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<AttachmentService> logger)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AttachmentResponseDto> UploadAsync(
        string entityType,
        int entityId,
        string category,
        IFormFile file,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("A non-empty file is required.", nameof(file));
        }

        var maxBytes = _configuration.GetValue("Files:MaxFileSizeBytes", 5L * 1024 * 1024);

        if (file.Length > maxBytes)
        {
            throw new ArgumentException(
                $"The file must be {maxBytes / (1024 * 1024)} MB or smaller.", nameof(file));
        }

        if (!AllowedCategories.TryGetValue(entityType, out var categories) ||
            !categories.Contains(category))
        {
            throw new ArgumentException("This attachment category is not supported.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedTypes.TryGetValue(extension, out var contentType))
        {
            throw new ArgumentException("Only PDF, JPG and PNG files are allowed.", nameof(file));
        }

        // CV uploads are PDF only.
        if (category == "ApplicationCv" && contentType != "application/pdf")
        {
            throw new ArgumentException("A CV must be a PDF file.", nameof(file));
        }

        await EnsureCanUploadAsync(entityType, entityId, category, actorEmployeeId, cancellationToken);

        // Read the file once, confirm the real content matches the extension.
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        if (!SignatureMatches(bytes, contentType))
        {
            throw new ArgumentException(
                "The file content does not match its file type.", nameof(file));
        }

        var storageRoot = Path.Combine(
            _environment.ContentRootPath,
            _configuration["Files:StorageRoot"] ?? "PrivateStorage");

        Directory.CreateDirectory(storageRoot);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(storageRoot, storedFileName);

        await File.WriteAllBytesAsync(physicalPath, bytes, cancellationToken);

        var attachment = new Attachment
        {
            UploadedByEmployeeId = actorEmployeeId,
            EntityType = entityType,
            EntityId = entityId,
            Category = category,
            // Strip any path the client may have sent.
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            StoragePath = physicalPath,
            ContentType = contentType,
            SizeBytes = bytes.LongLength,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            File.Delete(physicalPath);
            throw;
        }

        _logger.LogInformation(
            "Attachment {AttachmentId} uploaded for {EntityType} {EntityId}.",
            attachment.Id, entityType, entityId);

        return new AttachmentResponseDto
        {
            Id = attachment.Id,
            EntityType = attachment.EntityType,
            EntityId = attachment.EntityId,
            Category = attachment.Category,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            CreatedAtUtc = attachment.CreatedAtUtc
        };
    }

    public async Task<AttachmentDownload> GetDownloadAsync(
        int attachmentId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _context.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        if (attachment is null)
        {
            throw new KeyNotFoundException("Attachment was not found.");
        }

        var roles = await GetRolesAsync(actorEmployeeId, cancellationToken);
        var isHr = roles.Contains("HR");
        var isFinance = roles.Contains("Finance");

        var allowed = attachment.UploadedByEmployeeId == actorEmployeeId;

        if (!allowed)
        {
            switch (attachment.EntityType)
            {
                case TrainingRequestType:
                {
                    var training = await _context.TrainingRequests
                        .AsNoTracking()
                        .Where(t => t.Id == attachment.EntityId)
                        .Select(t => new { t.EmployeeId, t.AssignedReviewerId })
                        .FirstOrDefaultAsync(cancellationToken);

                    allowed = training is not null &&
                              (training.EmployeeId == actorEmployeeId ||
                               training.AssignedReviewerId == actorEmployeeId ||
                               isHr ||
                               isFinance);
                    break;
                }
                case KpiPlanType:
                {
                    var plan = await _context.KpiPlans
                        .AsNoTracking()
                        .Where(p => p.Id == attachment.EntityId)
                        .Select(p => new { p.EmployeeId, p.AssignedReviewerId })
                        .FirstOrDefaultAsync(cancellationToken);

                    allowed = plan is not null &&
                              (plan.EmployeeId == actorEmployeeId ||
                               plan.AssignedReviewerId == actorEmployeeId ||
                               isHr);
                    break;
                }
                case OpportunityType:
                    allowed = isHr;
                    break;
            }
        }

        if (!allowed)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to download this file.");
        }

        if (!File.Exists(attachment.StoragePath))
        {
            throw new KeyNotFoundException("The stored file could not be found.");
        }

        return new AttachmentDownload(
            attachment.StoragePath,
            attachment.ContentType,
            attachment.OriginalFileName);
    }

    private async Task EnsureCanUploadAsync(
        string entityType,
        int entityId,
        string category,
        int actorEmployeeId,
        CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case TrainingRequestType:
            {
                var training = await _context.TrainingRequests
                    .AsNoTracking()
                    .Where(t => t.Id == entityId)
                    .Select(t => new { t.EmployeeId, t.Status })
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new KeyNotFoundException("Training request not found.");

                if (training.EmployeeId != actorEmployeeId)
                {
                    throw new UnauthorizedAccessException(
                        "You can only upload files to your own training request.");
                }

                if (category == "SupportingDocument" &&
                    training.Status != TrainingStatus.Draft)
                {
                    throw new InvalidOperationException(
                        "Supporting documents can only be added while the request is a draft.");
                }

                break;
            }
            case KpiPlanType:
            {
                var plan = await _context.KpiPlans
                    .AsNoTracking()
                    .Where(p => p.Id == entityId)
                    .Select(p => new { p.EmployeeId })
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new KeyNotFoundException("KPI plan not found.");

                if (plan.EmployeeId != actorEmployeeId)
                {
                    throw new UnauthorizedAccessException(
                        "You can only upload evidence to your own KPI plan.");
                }

                break;
            }
            case OpportunityType:
            {
                var exists = await _context.Opportunities
                    .AsNoTracking()
                    .AnyAsync(o => o.Id == entityId &&
                                   o.Status == OpportunityStatus.Published,
                        cancellationToken);

                if (!exists)
                {
                    throw new KeyNotFoundException("Opportunity not found.");
                }

                break;
            }
        }
    }

    private async Task<HashSet<string>> GetRolesAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var names = await _context.EmployeeRoles
            .AsNoTracking()
            .Where(er => er.EmployeeId == employeeId)
            .Select(er => er.Role.Name)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    private static bool SignatureMatches(byte[] bytes, string contentType)
    {
        return contentType switch
        {
            "application/pdf" => bytes.Length > 4 &&
                                 bytes[0] == 0x25 && bytes[1] == 0x50 &&
                                 bytes[2] == 0x44 && bytes[3] == 0x46,
            "image/png" => bytes.Length > 8 &&
                           bytes[0] == 0x89 && bytes[1] == 0x50 &&
                           bytes[2] == 0x4E && bytes[3] == 0x47,
            "image/jpeg" => bytes.Length > 3 &&
                            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            _ => false
        };
    }
}
