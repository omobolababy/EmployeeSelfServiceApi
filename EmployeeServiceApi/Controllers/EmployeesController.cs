using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.EmployeeDto;
using EmployeeServiceApi.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1/employees")]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public class UpdatePhoneRequest
    {
        public string? PhoneNumber { get; set; }
    }

    public class AssignRolesRequest
    {
        public List<string> Roles { get; set; } = new();
    }

    public class SetStatusRequest
    {
        public EmployeeStatus Status { get; set; }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
        => Ok(await _employeeService.GetEmployeeByIdAsync(CurrentEmployeeId, ct));

    // Staff may change only their own phone number.
    [HttpPut("me/phone")]
    public async Task<IActionResult> UpdateMyPhone(
        [FromBody] UpdatePhoneRequest request, CancellationToken ct)
        => Ok(await _employeeService.UpdateMyPhoneNumberAsync(
            CurrentEmployeeId, request.PhoneNumber, ct));

    [Authorize(Roles = "Manager")]
    [HttpGet("my-team")]
    public async Task<IActionResult> GetMyTeam(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _employeeService.GetMyTeamAsync(CurrentEmployeeId, pagination, ct));

    [Authorize(Roles = "HR,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] PaginationRequestDto pagination, CancellationToken ct)
        => Ok(await _employeeService.GetEmployeesAsync(pagination, ct));

    [Authorize(Roles = "HR,Admin")]
    [HttpGet("{employeeId:int}")]
    public async Task<IActionResult> GetEmployee(int employeeId, CancellationToken ct)
        => Ok(await _employeeService.GetEmployeeByIdAsync(employeeId, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeDto request, CancellationToken ct)
    {
        var created = await _employeeService.CreateEmployeeAsync(request, CurrentEmployeeId, ct);
        return CreatedAtAction(nameof(GetEmployee), new { employeeId = created.Id }, created);
    }

    // Department, grade, reporting manager: HR or Admin only.
    [Authorize(Roles = "HR,Admin")]
    [HttpPut("{employeeId:int}")]
    public async Task<IActionResult> Update(
        int employeeId, [FromBody] UpdateEmployeeDto request, CancellationToken ct)
        => Ok(await _employeeService.UpdateEmployeeAsync(employeeId, request, CurrentEmployeeId, ct));

    [Authorize(Roles = "Admin")]
    [HttpPut("{employeeId:int}/roles")]
    public async Task<IActionResult> AssignRoles(
        int employeeId, [FromBody] AssignRolesRequest request, CancellationToken ct)
        => Ok(await _employeeService.AssignRolesAsync(
            employeeId, request.Roles, CurrentEmployeeId, ct));

    [Authorize(Roles = "Admin")]
    [HttpPut("{employeeId:int}/status")]
    public async Task<IActionResult> SetStatus(
        int employeeId, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        await _employeeService.SetEmployeeStatusAsync(
            employeeId, request.Status, CurrentEmployeeId, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{employeeId:int}/resend-setup-link")]
    public async Task<IActionResult> ResendSetupLink(int employeeId, CancellationToken ct)
    {
        await _employeeService.ResendSetupLinkAsync(employeeId, CurrentEmployeeId, ct);
        return Ok(new { responseMessage = "A new setup link has been queued." });
    }
}
