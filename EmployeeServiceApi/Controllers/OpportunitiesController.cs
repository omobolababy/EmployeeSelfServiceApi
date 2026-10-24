using EmployeeSelfServiceApi.DTOs.Opportunities;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1")]
public class OpportunitiesController : ApiControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly IAttachmentService _attachmentService;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        IAttachmentService attachmentService)
    {
        _opportunityService = opportunityService;
        _attachmentService = attachmentService;
    }

    // ---- HR ----

    [Authorize(Roles = "HR")]
    [HttpPost("opportunities")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOpportunityDto request, CancellationToken ct)
        => Ok(await _opportunityService.CreateOpportunityAsync(request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPut("opportunities/{opportunityId:int}")]
    public async Task<IActionResult> Update(
        int opportunityId, [FromBody] UpdateOpportunityDto request, CancellationToken ct)
        => Ok(await _opportunityService.UpdateOpportunityAsync(
            opportunityId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("opportunities/{opportunityId:int}/publish")]
    public async Task<IActionResult> Publish(
        int opportunityId, [FromBody] PublishOpportunityDto request, CancellationToken ct)
        => Ok(await _opportunityService.PublishOpportunityAsync(
            opportunityId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("opportunities/{opportunityId:int}/close")]
    public async Task<IActionResult> Close(int opportunityId, CancellationToken ct)
        => Ok(await _opportunityService.CloseOpportunityAsync(opportunityId, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpGet("opportunities/{opportunityId:int}/applications")]
    public async Task<IActionResult> GetApplicants(
        int opportunityId, [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _opportunityService.GetApplicantsAsync(
            opportunityId, pagination, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPut("applications/{applicationId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int applicationId, [FromBody] UpdateApplicationStatusDto request, CancellationToken ct)
        => Ok(await _opportunityService.UpdateApplicationStatusAsync(
            applicationId, request, CurrentEmployeeId, ct));

    // ---- Everyone ----

    /// Staff see published, open records, HR sees all
    [HttpGet("opportunities")]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _opportunityService.GetOpenOpportunitiesAsync(
            pagination, CurrentEmployeeId, ct));

    [HttpGet("opportunities/{opportunityId:int}")]
    public async Task<IActionResult> Get(int opportunityId, CancellationToken ct)
        => Ok(await _opportunityService.GetOpportunityByIdAsync(
            opportunityId, CurrentEmployeeId, ct));

    /// Optional CV upload (PDF only, 5 MB). Pass the returned id when applying
    [HttpPost("opportunities/{opportunityId:int}/cv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadCv(
        int opportunityId, IFormFile file, CancellationToken ct)
        => Ok(await _attachmentService.UploadAsync(
            "Opportunity", opportunityId, "ApplicationCv", file, CurrentEmployeeId, ct));

    [HttpPost("opportunities/{opportunityId:int}/applications")]
    public async Task<IActionResult> Apply(
        int opportunityId, [FromBody] ApplyForOpportunityDto request, CancellationToken ct)
        => Ok(await _opportunityService.ApplyForOpportunityAsync(
            opportunityId, request, CurrentEmployeeId, ct));

    [HttpGet("applications/my")]
    public async Task<IActionResult> MyApplications(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _opportunityService.GetMyApplicationsAsync(
            CurrentEmployeeId, pagination, ct));

    [HttpGet("applications/{applicationId:int}")]
    public async Task<IActionResult> GetApplication(int applicationId, CancellationToken ct)
        => Ok(await _opportunityService.GetApplicationByIdAsync(
            applicationId, CurrentEmployeeId, ct));

    [HttpPost("applications/{applicationId:int}/withdraw")]
    public async Task<IActionResult> Withdraw(
        int applicationId, [FromBody] WithdrawApplicationDto request, CancellationToken ct)
        => Ok(await _opportunityService.WithdrawApplicationAsync(
            applicationId, request, CurrentEmployeeId, ct));
}
