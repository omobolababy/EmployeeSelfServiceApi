using EmployeeServiceApi.DTOs;
using EmployeeSelfServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;

[Route("api/v1/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request, CancellationToken ct)
        => Ok(await _authService.LoginAsync(request, ct));

    [AllowAnonymous]
    [HttpPost("activate")]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateAccountRequestDto request, CancellationToken ct)
    {
        await _authService.ActivateAccountAsync(request, ct);
        return Ok(new { responseMessage = "Account activated. You can now sign in." });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
    {
        await _authService.RequestPasswordResetAsync(request, ct);

        // Same response whether or not the email exists.
        return Ok(new
        {
            responseMessage = "If the email is registered, a reset message has been sent."
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok(new { responseMessage = "Password reset successfully." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request, CancellationToken ct)
    {
        await _authService.ChangePasswordAsync(CurrentEmployeeId, request, ct);
        return Ok(new { responseMessage = "Password changed successfully." });
    }
}
