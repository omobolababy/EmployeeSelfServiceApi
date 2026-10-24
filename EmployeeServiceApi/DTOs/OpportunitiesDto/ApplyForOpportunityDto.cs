namespace EmployeeSelfServiceApi.DTOs.Opportunities;

public class ApplyForOpportunityDto
{
    public string? CoverLetter { get; set; }

    // The server validates the uploaded attachment separately.
    public int? AttachmentId { get; set; }
}