using EmployeeSelfServiceApi.DTOs.KPI;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.KPI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1/kpi")]
public class KpiController : ApiControllerBase
{
    private readonly IKpiService _kpiService;
    private readonly IAttachmentService _attachmentService;

    public KpiController(IKpiService kpiService, IAttachmentService attachmentService)
    {
        _kpiService = kpiService;
        _attachmentService = attachmentService;
    }

    [Authorize(Roles = "HR")]
    [HttpPost("review-periods")]
    public async Task<IActionResult> CreateReviewPeriod(
        [FromBody] CreateReviewPeriodDto request, CancellationToken ct)
        => Ok(await _kpiService.CreateReviewPeriodAsync(request, CurrentEmployeeId, ct));

    /// HR creates a Draft plan for an employee (never for themselves)
    [Authorize(Roles = "HR")]
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreateKpiPlanDto request, CancellationToken ct)
        => Ok(await _kpiService.CreatePlanAsync(request, CurrentEmployeeId, ct));

    /// HR edits targets and weights while the plan is Draft
    [Authorize(Roles = "HR")]
    [HttpPut("plans/{planId:int}")]
    public async Task<IActionResult> UpdatePlan(
        int planId, [FromBody] UpdateKpiPlanDto request, CancellationToken ct)
        => Ok(await _kpiService.UpdatePlanAsync(planId, request, CurrentEmployeeId, ct));

    /// HR activates a Draft plan (weights must total exactly 100.00)
    [Authorize(Roles = "HR")]
    [HttpPost("plans/{planId:int}/activate")]
    public async Task<IActionResult> ActivatePlan(
        int planId, [FromBody] ActivateKpiPlanDto request, CancellationToken ct)
        => Ok(await _kpiService.ActivatePlanAsync(planId, request, CurrentEmployeeId, ct));

    /// Employee records actual values and evidence comments (Active/Returned, before the deadline)
    [HttpPut("plans/{planId:int}/progress")]
    public async Task<IActionResult> UpdateProgress(
        int planId, [FromBody] UpdateKpiProgressDto request, CancellationToken ct)
        => Ok(await _kpiService.UpdateProgressAsync(planId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR,Admin")]
    [HttpGet("plans")]
    public async Task<IActionResult> GetAllPlans(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _kpiService.GetAllPlansAsync(pagination, CurrentEmployeeId, ct));

    [HttpPost("plans/{planId:int}/submit")]
    public async Task<IActionResult> SubmitPlan(int planId, CancellationToken ct)
        => Ok(await _kpiService.SubmitPlanAsync(planId, CurrentEmployeeId, ct));

    [HttpGet("plans/my")]
    public async Task<IActionResult> GetMyPlans(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _kpiService.GetMyPlansAsync(CurrentEmployeeId, pagination, ct));

    [Authorize(Roles = "Manager")]
    [HttpGet("plans/review-queue")]
    public async Task<IActionResult> GetReviewQueue(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _kpiService.GetReviewQueueAsync(CurrentEmployeeId, pagination, ct));

    [HttpGet("plans/{planId:int}")]
    public async Task<IActionResult> GetPlan(int planId, CancellationToken ct)
        => Ok(await _kpiService.GetPlanAsync(planId, CurrentEmployeeId, ct));

    /// Approve = finalize with a server-calculated score; Return needs a comment
    [Authorize(Roles = "Manager")]
    [HttpPost("plans/{planId:int}/review")]
    public async Task<IActionResult> ReviewPlan(
        int planId, [FromBody] ReviewKpiPlanDto request, CancellationToken ct)
        => Ok(await _kpiService.ReviewPlanAsync(planId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("plans/{planId:int}/reopen")]
    public async Task<IActionResult> ReopenPlan(
        int planId, [FromBody] ReopenKpiPlanDto request, CancellationToken ct)
        => Ok(await _kpiService.ReopenPlanAsync(planId, request, CurrentEmployeeId, ct));

    /// Upload an evidence file (PDF/JPG/PNG, 5 MB max) for your own plan
    [HttpPost("plans/{planId:int}/evidence")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadEvidence(
        int planId, IFormFile file, CancellationToken ct)
        => Ok(await _attachmentService.UploadAsync(
            "KpiPlan", planId, "KpiEvidence", file, CurrentEmployeeId, ct));
}
