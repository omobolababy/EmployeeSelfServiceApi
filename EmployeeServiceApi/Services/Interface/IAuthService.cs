using EmployeeServiceApi.DTOs;

namespace EmployeeSelfServiceApi.Services.Interfaces;

public interface IAuthService
{
    Task<AuthTokenResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task ActivateAccountAsync(
        ActivateAccountRequestDto request,
        CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        int employeeId,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default);
}
