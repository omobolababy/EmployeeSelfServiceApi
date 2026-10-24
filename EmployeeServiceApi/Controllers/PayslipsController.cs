using EmployeeSelfServiceApi.DTOs.Payroll;
using EmployeeSelfServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1/payslips")]
public class PayslipsController : ApiControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly IPayslipPdfService _pdfService;

    public PayslipsController(IPayrollService payrollService, IPayslipPdfService pdfService)
    {
        _payrollService = payrollService;
        _pdfService = pdfService;
    }

    // ---- Finance ----

    [Authorize(Roles = "Finance")]
    [HttpPost]
    public async Task<IActionResult> Prepare(
        [FromBody] CreatePayslipDto request, CancellationToken ct)
        => Ok(await _payrollService.CreatePayslipAsync(request, CurrentEmployeeId, ct));

    [Authorize(Roles = "Finance")]
    [HttpPost("{payslipId:int}/revisions")]
    public async Task<IActionResult> CreateRevision(
        int payslipId, [FromBody] RevisePayslipDto request, CancellationToken ct)
        => Ok(await _payrollService.RevisePayslipAsync(payslipId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "Finance")]
    [HttpPost("{payslipId:int}/versions/{versionId:int}/publish")]
    public async Task<IActionResult> Publish(
        int payslipId, int versionId, CancellationToken ct)
        => Ok(await _payrollService.PublishPayslipAsync(payslipId, versionId, CurrentEmployeeId, ct));

    [Authorize(Roles = "Finance")]
    [HttpGet("{payslipId:int}")]
    public async Task<IActionResult> GetForFinance(int payslipId, CancellationToken ct)
        => Ok(await _payrollService.GetPayslipForFinanceAsync(payslipId, CurrentEmployeeId, ct));

    // ---- Staff (own payslips only; drafts are never visible) ---------

    [HttpGet("my")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
    {
        var payslips = await _payrollService.GetMyPayslipsAsync(CurrentEmployeeId, ct);

        var filtered = payslips
            .Where(p => (!month.HasValue || p.PayrollMonth == month.Value) &&
                        (!year.HasValue || p.PayrollYear == year.Value))
            .ToList();

        return Ok(filtered);
    }

    [HttpGet("my/{payslipId:int}")]
    public async Task<IActionResult> GetMine(int payslipId, CancellationToken ct)
        => Ok(await _payrollService.GetMyPayslipAsync(payslipId, CurrentEmployeeId, ct));

    [HttpGet("{payslipId:int}/versions/{versionId:int}")]
    public async Task<IActionResult> GetVersion(
        int payslipId, int versionId, CancellationToken ct)
        => Ok(await _payrollService.GetPublishedVersionAsync(
            payslipId, versionId, CurrentEmployeeId, ct));

    
    /// Owner and Finance can download the current or any historical published
    /// version. Everyone else (including HR and Managers) gets 403.
    
    [HttpGet("{payslipId:int}/versions/{versionId:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        int payslipId, int versionId, CancellationToken ct)
    {
        var version = await _payrollService.GetPublishedVersionAsync(
            payslipId, versionId, CurrentEmployeeId, ct);

        PayslipResponseDto payslip;

        try
        {
            payslip = await _payrollService.GetMyPayslipAsync(payslipId, CurrentEmployeeId, ct);
        }
        catch (UnauthorizedAccessException)
        {
            // Not the owner: only Finance may continue (service throws 403 otherwise).
            payslip = await _payrollService.GetPayslipForFinanceAsync(
                payslipId, CurrentEmployeeId, ct);
        }

        var pdf = _pdfService.Generate(payslip, version);

        return File(
            pdf,
            "application/pdf",
            $"payslip-{payslip.PayrollYear}-{payslip.PayrollMonth:D2}-v{version.VersionNumber}.pdf");
    }
}
