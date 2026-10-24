
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.Entities;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.DTOs;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using EmployeeSelfServiceApi.Exceptions;
using EmployeeSelfServiceApi.Utilities;

namespace EmployeeSelfServiceApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    private const int MaximumLoginAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int TokenExpiryHours = 48;
    private const int DefaultResetTokenExpiryMinutes = 60;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthTokenResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var employee = await _context.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(
                    e => e.Email.ToLower() == email,
                    cancellationToken);

            // One generic message for unknown email / inactive account so the
            // response never reveals which accounts exist.
            if (employee is null || employee.Status != EmployeeStatus.Active)
            {
                throw new ApiException(
                    StatusCodes.Status401Unauthorized,
                    "Invalid email or password.");
            }

            var now = DateTime.UtcNow;

            if (employee.LockedUntilUtc.HasValue &&
                employee.LockedUntilUtc.Value > now)
            {
                throw new ApiException(
                    StatusCodes.Status401Unauthorized,
                    "Your account is temporarily locked. Please try again later.");
            }

            // Start a new failed-login window after the previous lockout
            // has expired.
            if (employee.LockedUntilUtc.HasValue &&
                employee.LockedUntilUtc.Value <= now)
            {
                employee.FailedLoginAttempts = 0;
                employee.FirstFailedLoginAtUtc = null;
                employee.LockedUntilUtc = null;
            }

            if (employee.FirstFailedLoginAtUtc.HasValue &&
                employee.FirstFailedLoginAtUtc.Value
                    .AddMinutes(LockoutMinutes) <= now)
            {
                employee.FailedLoginAttempts = 0;
                employee.FirstFailedLoginAtUtc = null;
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
            {
                if (!employee.FirstFailedLoginAtUtc.HasValue)
                {
                    employee.FirstFailedLoginAtUtc = now;
                }

                employee.FailedLoginAttempts++;

                if (employee.FailedLoginAttempts >= MaximumLoginAttempts)
                {
                    employee.LockedUntilUtc =
                        now.AddMinutes(LockoutMinutes);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Failed login attempt for employee ID {EmployeeId}.",
                    employee.Id);

                throw new ApiException(
                    StatusCodes.Status401Unauthorized,
                    "Invalid email or password.");
            }

            employee.FailedLoginAttempts = 0;
            employee.FirstFailedLoginAtUtc = null;
            employee.LockedUntilUtc = null;

            await _context.SaveChangesAsync(cancellationToken);

            var roles = employee.EmployeeRoles
                .Select(er => er.Role.Name)
                .Distinct()
                .ToList();

            return GenerateToken(employee, roles);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login.");
            throw;
        }
    }

    public async Task ActivateAccountAsync(
        ActivateAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePassword(request.NewPassword);

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException(
                    "Passwords do not match.");
            }

            var tokenHash = HashToken(request.Token);
            var now = DateTime.UtcNow;

            var setupToken = await _context.EmployeeSetupTokens
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(
                    t => t.TokenHash == tokenHash,
                    cancellationToken);

            if (setupToken is null ||
                setupToken.UsedAtUtc.HasValue ||
                setupToken.ExpiresAtUtc <= now)
            {
                throw new ArgumentException(
                    "The setup link is invalid or has expired.");
            }

            var employee = setupToken.Employee;

            if (employee.Status != EmployeeStatus.PendingActivation)
            {
                throw new InvalidOperationException(
                    "This account cannot be activated.");
            }

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

            employee.Status = EmployeeStatus.Active;
            employee.ActivatedAtUtc = now;
            employee.PasswordChangedAtUtc = now;

            setupToken.UsedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Employee account activated. Employee ID: {EmployeeId}.",
                employee.Id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Account activation failed.");
            throw;
        }
    }

    public async Task RequestPasswordResetAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.Email.ToLower() == email &&
                         e.Status == EmployeeStatus.Active,
                    cancellationToken);

            // Do not reveal whether an email belongs to an account.
            if (employee is null)
            {
                _logger.LogInformation(
                    "Password reset requested for an unrecognized account.");
                return;
            }

            var rawToken = GenerateSecureToken();
            var now = DateTime.UtcNow;
            var resetExpiryMinutes = _configuration.GetValue(
                "App:ResetTokenExpiryMinutes", DefaultResetTokenExpiryMinutes);

            var resetToken = new PasswordResetToken
            {
                EmployeeId = employee.Id,
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(resetExpiryMinutes)
            };

            _context.PasswordResetTokens.Add(resetToken);

            var baseUrl = _configuration["App:FrontendBaseUrl"];

            var resetLink = string.IsNullOrWhiteSpace(baseUrl)
                ? rawToken
                : $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";

            _context.EmailQueueItems.Add(EmailTemplates.ToQueueItem(
                employee.Email,
                "Reset Your Employee Self-Service Password",
                EmailTemplates.PasswordReset(
                    employee.FirstName,
                    rawToken,
                    string.IsNullOrWhiteSpace(baseUrl) ? null : resetLink,
                    resetExpiryMinutes),
                now));

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Password reset request processed for employee ID {EmployeeId}.",
                employee.Id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Password reset request failed.");
            throw;
        }
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePassword(request.NewPassword);

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException(
                    "Passwords do not match.");
            }

            var tokenHash = HashToken(request.Token);
            var now = DateTime.UtcNow;

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(
                    t => t.TokenHash == tokenHash,
                    cancellationToken);

            if (resetToken is null ||
                resetToken.UsedAtUtc.HasValue ||
                resetToken.ExpiresAtUtc <= now)
            {
                throw new ArgumentException(
                    "The reset link is invalid or has expired.");
            }

            var employee = resetToken.Employee;

            if (employee.Status != EmployeeStatus.Active)
            {
                throw new InvalidOperationException(
                    "This account cannot reset its password.");
            }

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

            employee.PasswordChangedAtUtc = now;
            employee.FailedLoginAttempts = 0;
            employee.FirstFailedLoginAtUtc = null;
            employee.LockedUntilUtc = null;

            resetToken.UsedAtUtc = now;

            // Invalidate any other unused reset links for this employee.
            var otherTokens = await _context.PasswordResetTokens
                .Where(t => t.EmployeeId == employee.Id &&
                            t.Id != resetToken.Id &&
                            t.UsedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in otherTokens)
            {
                token.UsedAtUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Password reset completed for employee ID {EmployeeId}.",
                employee.Id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Password reset failed.");
            throw;
        }
    }

    public async Task ChangePasswordAsync(
        int employeeId,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.NewPassword);

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Id == employeeId && e.Status == EmployeeStatus.Active,
                cancellationToken);

        if (employee is null)
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "The account is not active.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, employee.PasswordHash))
        {
            throw new ArgumentException("The current password is incorrect.");
        }

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        employee.PasswordChangedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private AuthTokenResponseDto GenerateToken(
        Employee employee,
        List<string> roles)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var expiryMinutes = _configuration.GetValue<int>(
            "Jwt:ExpiryMinutes", 60);

        if (expiryMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT expiry must be greater than zero.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "The JWT signing key must be at least 32 bytes.");
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name,
                $"{employee.FirstName} {employee.LastName}"),
            new(ClaimTypes.Email, employee.Email),
            new("EmployeeNumber", employee.EmployeeNumber)
        };

        // Every active employee is implicitly Staff.
        claims.AddRange(roles
            .Append("Staff")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthTokenResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAt,
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Email = employee.Email,
            Roles = roles
                .Append("Staff")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 10 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new ArgumentException(
                "Password must be at least 10 characters long " +
                "and contain an uppercase letter, a lowercase letter, " +
                "and a digit.");
        }
    }
}
