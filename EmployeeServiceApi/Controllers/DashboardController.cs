using EmployeeSelfServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// Personal summary for every signed-in employee
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await _dashboardService.GetEmployeeDashboardAsync(CurrentEmployeeId, ct));

    [Authorize(Roles = "Manager")]
    [HttpGet("manager")]
    public async Task<IActionResult> Manager(CancellationToken ct)
        => Ok(await _dashboardService.GetManagerDashboardAsync(CurrentEmployeeId, ct));

    [Authorize(Roles = "HR")]
    [HttpGet("hr")]
    public async Task<IActionResult> Hr(CancellationToken ct)
        => Ok(await _dashboardService.GetHrDashboardAsync(ct));

    [Authorize(Roles = "Finance")]
    [HttpGet("finance")]
    public async Task<IActionResult> Finance(CancellationToken ct)
        => Ok(await _dashboardService.GetFinanceDashboardAsync(ct));

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<IActionResult> Admin(CancellationToken ct)
        => Ok(await _dashboardService.GetAdminDashboardAsync(ct));
}
