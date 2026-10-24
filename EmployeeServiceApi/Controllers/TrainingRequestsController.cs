using EmployeeSelfServiceApi.DTOs.Training;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.Training;
using EmployeeServiceApi.DTOs.TrainingDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1/training-requests")]
public class TrainingRequestsController : ApiControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly IAttachmentService _attachmentService;

    public TrainingRequestsController(
        ITrainingService trainingService,
        IAttachmentService attachmentService)
    {
        _trainingService = trainingService;
        _attachmentService = attachmentService;
    }

    public class CloseNoEvidenceRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    // ---- Staff ----

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTrainingRequestDto request, CancellationToken ct)
    {
        var created = await _trainingService.CreateRequestAsync(request, CurrentEmployeeId, ct);
        return CreatedAtAction(nameof(GetById), new { requestId = created.Id }, created);
    }

    [HttpPut("{requestId:int}")]
    public async Task<IActionResult> Update(
        int requestId, [FromBody] UpdateTrainingRequestDto request, CancellationToken ct)
        => Ok(await _trainingService.UpdateRequestAsync(requestId, request, CurrentEmployeeId, ct));

    [HttpPost("{requestId:int}/submit")]
    public async Task<IActionResult> Submit(int requestId, CancellationToken ct)
        => Ok(await _trainingService.SubmitRequestAsync(requestId, CurrentEmployeeId, ct));

    [HttpGet("my")]
    public async Task<IActionResult> GetMine(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _trainingService.GetMyRequestsAsync(CurrentEmployeeId, pagination, ct));

    [HttpGet("{requestId:int}")]
    public async Task<IActionResult> GetById(int requestId, CancellationToken ct)
        => Ok(await _trainingService.GetRequestByIdAsync(requestId, CurrentEmployeeId, ct));

    [HttpPost("{requestId:int}/withdraw")]
    public async Task<IActionResult> Withdraw(
        int requestId, [FromBody] WithdrawTrainingRequestDto request, CancellationToken ct)
    {
        await _trainingService.WithdrawRequestAsync(requestId, request, CurrentEmployeeId, ct);
        return NoContent();
    }

    /// Upload a supporting document (PDF/JPG/PNG, 5 MB max) to a draft
    [HttpPost("{requestId:int}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadSupportingDocument(
        int requestId, IFormFile file, CancellationToken ct)
        => Ok(await _attachmentService.UploadAsync(
            "TrainingRequest", requestId, "SupportingDocument", file, CurrentEmployeeId, ct));

    /// Upload the completion certificate/report and submit it for HR review
    [HttpPost("{requestId:int}/evidence")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitEvidence(
        int requestId, IFormFile file, CancellationToken ct)
    {
        var attachment = await _attachmentService.UploadAsync(
            "TrainingRequest", requestId, "CompletionEvidence", file, CurrentEmployeeId, ct);

        var evidence = await _trainingService.SubmitCompletionEvidenceAsync(
            requestId, attachment.Id, CurrentEmployeeId, ct);

        // Mapped here so the entity graph is never serialised.
        return Ok(new
        {
            evidenceId = evidence.Id,
            trainingRequestId = requestId,
            attachmentId = attachment.Id,
            status = evidence.Status.ToString()
        });
    }

    // ---- Decisions (manager / HR / Finance, enforced by the service) ---

    [HttpPost("{requestId:int}/decision")]
    public async Task<IActionResult> Decide(
        int requestId, [FromBody] TrainingDecisionDto request, CancellationToken ct)
        => Ok(await _trainingService.DecideAsync(requestId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("{requestId:int}/allowance/corrections")]
    public async Task<IActionResult> CorrectAllowance(
        int requestId, [FromBody] CorrectTrainingAllowanceDto request, CancellationToken ct)
        => Ok(await _trainingService.CorrectAllowanceAsync(requestId, request, CurrentEmployeeId, ct));

    [HttpGet("{requestId:int}/allowance")]
    public async Task<IActionResult> GetAllowance(int requestId, CancellationToken ct)
        => Ok(await _trainingService.GetAllowanceVersionsAsync(requestId, CurrentEmployeeId, ct));

    /// Allowance PDF, built from the approved version
    [HttpGet("{requestId:int}/allowance/pdf")]
    public async Task<IActionResult> DownloadAllowancePdf(int requestId, CancellationToken ct)
    {
        var (content, fileName) = await _trainingService.GenerateAllowancePdfAsync(
            requestId, CurrentEmployeeId, ct);

        return File(content, "application/pdf", fileName);
    }

    [Authorize(Roles = "HR")]
    [HttpPost("{requestId:int}/evidence/{evidenceId:int}/review")]
    public async Task<IActionResult> ReviewEvidence(
        int requestId, int evidenceId,
        [FromBody] ReviewTrainingEvidenceDto request, CancellationToken ct)
        => Ok(await _trainingService.ReviewCompletionEvidenceAsync(
            requestId, evidenceId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpPost("{requestId:int}/close-no-evidence")]
    public async Task<IActionResult> CloseWithoutEvidence(
        int requestId, [FromBody] CloseNoEvidenceRequest request, CancellationToken ct)
        => Ok(await _trainingService.CloseWithoutEvidenceAsync(
            requestId, request.Reason, CurrentEmployeeId, ct));

    // ---- Admin ---------------------------------------------------------

    [Authorize(Roles = "Admin")]
    [HttpPost("{requestId:int}/reassign-reviewer")]
    public async Task<IActionResult> ReassignReviewer(
        int requestId, [FromBody] ReassignTrainingReviewerDto request, CancellationToken ct)
        => Ok(await _trainingService.ReassignReviewerAsync(requestId, request, CurrentEmployeeId, ct));

    // ---- Queues --------------------------------------------------------

    [Authorize(Roles = "Manager")]
    [HttpGet("pending-manager")]
    public async Task<IActionResult> PendingManager(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _trainingService.GetManagerQueueAsync(CurrentEmployeeId, pagination, ct));

    [Authorize(Roles = "Admin")]
    [HttpGet("pending-manager/all")]
    public async Task<IActionResult> PendingManagerAll(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _trainingService.GetAllPendingManagerAsync(CurrentEmployeeId, pagination, ct));

    [Authorize(Roles = "HR")]
    [HttpGet("pending-hr")]
    public async Task<IActionResult> PendingHr(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _trainingService.GetHrQueueAsync(CurrentEmployeeId, pagination, ct));

    [Authorize(Roles = "Finance")]
    [HttpGet("pending-finance")]
    public async Task<IActionResult> PendingFinance(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _trainingService.GetFinanceQueueAsync(CurrentEmployeeId, pagination, ct));
}

[Route("api/v1/estacode-rates")]
[Authorize(Roles = "HR,Admin")]
public class EstacodeRatesController : ApiControllerBase
{
    private readonly ITrainingService _trainingService;

    public EstacodeRatesController(ITrainingService trainingService)
    {
        _trainingService = trainingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEstacodeRateDto request, CancellationToken ct)
        => Ok(await _trainingService.CreateEstacodeRateAsync(request, CurrentEmployeeId, ct));

    [HttpPut("{rateId:int}")]
    public async Task<IActionResult> Update(
        int rateId, [FromBody] UpdateEstacodeRateDto request, CancellationToken ct)
        => Ok(await _trainingService.UpdateEstacodeRateAsync(rateId, request, CurrentEmployeeId, ct));
}
