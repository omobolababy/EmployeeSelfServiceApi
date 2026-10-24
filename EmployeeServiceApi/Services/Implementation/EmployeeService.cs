
using System.Security.Cryptography;
using System.Text;
using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Employees;
using EmployeeSelfServiceApi.Entities;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.EmployeeDto;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<EmployeeService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PagedResponseDto<EmployeeListItemDto>>
        GetEmployeesAsync(
            PaginationRequestDto pagination,
            CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.EmployeeNumber);

            var totalCount = await query.CountAsync(cancellationToken);

            var employees = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .ToListAsync(cancellationToken);

            var results = employees.Select(e => new EmployeeListItemDto
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,
                FullName = $"{e.FirstName} {e.LastName}",
                Email = e.Email,
                Department = e.Department,
                Grade = e.Grade,
                Status = e.Status.ToString(),
                Roles = e.EmployeeRoles
                    .Select(er => er.Role.Name)
                    .ToList()
            }).ToList();

            return PaginationService.CreateResponse(
                results,
                pagination.PageNumber,
                pagination.PageSize,
                totalCount);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to retrieve employees.");
            throw;
        }
    }

    public async Task<EmployeeProfileDto> GetEmployeeByIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .Include(e => e.ReportingManager)
                .FirstOrDefaultAsync(
                    e => e.Id == employeeId,
                    cancellationToken);

            if (employee is null)
            {
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found.");
            }

            return MapProfile(employee);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve employee {EmployeeId}.",
                employeeId);
            throw;
        }
    }

    public async Task<EmployeeProfileDto> CreateEmployeeAsync(
        CreateEmployeeDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var employeeNumber = request.EmployeeNumber.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            var duplicate = await _context.Employees.AnyAsync(
                e => e.EmployeeNumber == employeeNumber ||
                     e.Email.ToLower() == email,
                cancellationToken);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "An employee with this employee number or email already exists.");
            }

            if (request.Roles is null || request.Roles.Count == 0)
            {
                throw new ArgumentException(
                    "At least one role must be assigned.");
            }

            var roleNames = request.Roles
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (roleNames.Count != request.Roles.Count)
            {
                throw new ArgumentException(
                    "Duplicate roles are not allowed.");
            }

            var roles = await _context.Roles
                .Where(r => roleNames.Contains(r.Name))
                .ToListAsync(cancellationToken);

            if (roles.Count != roleNames.Count)
            {
                throw new ArgumentException(
                    "One or more requested roles do not exist.");
            }

            var managerId = request.ReportingManagerId;

            if (managerId.HasValue)
            {
                var managerExists = await _context.Employees
                    .AnyAsync(
                        e => e.Id == managerId.Value &&
                             e.Status == EmployeeStatus.Active,
                        cancellationToken);

                if (!managerExists)
                {
                    throw new ArgumentException(
                    "The selected reporting manager does not exist or is inactive.");
                }
            }

            var now = DateTime.UtcNow;

            var employee = new Employee
            {
                EmployeeNumber = employeeNumber,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Department = request.Department.Trim(),
                Grade = request.Grade.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim(),
                ReportingManagerId = managerId,
                Status = EmployeeStatus.PendingActivation,
                PasswordHash = string.Empty,
                FailedLoginAttempts = 0,
                CreatedAtUtc = now
            };

            foreach (var role in roles)
            {
                employee.EmployeeRoles.Add(new EmployeeRoles
                {
                    Role = role
                });
            }

            _context.Employees.Add(employee);

            // Save first so the employee has a database-generated ID.
            await _context.SaveChangesAsync(cancellationToken);

            AddSetupTokenAndEmail(employee, now);

            _context.AuditLogs.Add(new AuditLog
            {
                ActorEmployeeId = actorEmployeeId,
                Action = "EmployeeCreated",
                EntityType = "Employee",
                EntityId = employee.Id.ToString(),
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        employee.EmployeeNumber,
                        Roles = roleNames
                    }),
                CreatedAtUtc = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Employee {EmployeeId} was created by actor {ActorEmployeeId}.",
                employee.Id,
                actorEmployeeId);

            return await GetEmployeeByIdAsync(
                employee.Id,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to create an employee.");

            throw;
        }
    }

    public async Task<EmployeeProfileDto> UpdateEmployeeAsync(
        int employeeId,
        UpdateEmployeeDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(
                    e => e.Id == employeeId,
                    cancellationToken);

            if (employee is null)
            {
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found.");
            }

            if (request.ReportingManagerId == employeeId)
            {
                throw new ArgumentException(
                    "An employee cannot report to themselves.");
            }

            if (request.ReportingManagerId.HasValue)
            {
                var managerExists = await _context.Employees
                    .AnyAsync(
                        e => e.Id == request.ReportingManagerId.Value &&
                             e.Status == EmployeeStatus.Active,
                        cancellationToken);

                if (!managerExists)
                {
                    throw new ArgumentException(
                    "The selected reporting manager does not exist or is inactive.");
                }
            }

            if (request.ReportingManagerId.HasValue &&
                await CreatesReportingCycleAsync(
                    employeeId, request.ReportingManagerId.Value, cancellationToken))
            {
                throw new ArgumentException(
                    "This change would create a reporting cycle.");
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                employee.FirstName = request.FirstName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                employee.LastName = request.LastName.Trim();
            }

            employee.Department = request.Department.Trim();
            employee.Grade = request.Grade.Trim();
            employee.PhoneNumber =
                string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim();
            employee.ReportingManagerId = request.ReportingManagerId;

            _context.AuditLogs.Add(new AuditLog
            {
                ActorEmployeeId = actorEmployeeId,
                Action = "EmployeeUpdated",
                EntityType = "Employee",
                EntityId = employee.Id.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Employee {EmployeeId} was updated by actor {ActorEmployeeId}.",
                employeeId,
                actorEmployeeId);

            return await GetEmployeeByIdAsync(
                employeeId,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to update employee {EmployeeId}.",
                employeeId);
            throw;
        }
    }

    public async Task SetEmployeeStatusAsync(
        int employeeId,
        EmployeeStatus status,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.IsDefined(typeof(EmployeeStatus), status))
            {
                throw new ArgumentException(
                    "The requested employee status is invalid.");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.Id == employeeId,
                    cancellationToken);

            if (employee is null)
            {
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found.");
            }

            if (employeeId == actorEmployeeId &&
                status != EmployeeStatus.Active)
            {
                throw new InvalidOperationException(
                    "You cannot deactivate your own account through this operation.");
            }

            var previousStatus = employee.Status;
            employee.Status = status;

            if (status != EmployeeStatus.Active)
            {
                employee.LockedUntilUtc = null;
                employee.FailedLoginAttempts = 0;
                employee.FirstFailedLoginAtUtc = null;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                ActorEmployeeId = actorEmployeeId,
                Action = "EmployeeStatusChanged",
                EntityType = "Employee",
                EntityId = employee.Id.ToString(),
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        PreviousStatus = previousStatus.ToString(),
                        NewStatus = status.ToString()
                    }),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Employee {EmployeeId} status changed from {PreviousStatus} to {NewStatus} by actor {ActorEmployeeId}.",
                employeeId,
                previousStatus,
                status,
                actorEmployeeId);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to change status for employee {EmployeeId}.",
                employeeId);
            throw;
        }
    }

    public async Task<EmployeeProfileDto> UpdateMyPhoneNumberAsync(
        int employeeId,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new KeyNotFoundException("Employee was not found.");

        var trimmed = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        if (trimmed is { Length: > 30 })
        {
            throw new ArgumentException("The phone number is too long.");
        }

        employee.PhoneNumber = trimmed;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetEmployeeByIdAsync(employeeId, cancellationToken);
    }

    public async Task<PagedResponseDto<EmployeeListItemDto>> GetMyTeamAsync(
        int managerId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        // Only employees whose CURRENT reporting manager is this user.
        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.ReportingManagerId == managerId)
            .OrderBy(e => e.EmployeeNumber);

        var totalCount = await query.CountAsync(cancellationToken);

        var employees = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Include(e => e.EmployeeRoles)
                .ThenInclude(er => er.Role)
            .ToListAsync(cancellationToken);

        var results = employees.Select(e => new EmployeeListItemDto
        {
            Id = e.Id,
            EmployeeNumber = e.EmployeeNumber,
            FullName = $"{e.FirstName} {e.LastName}",
            Email = e.Email,
            Department = e.Department,
            Grade = e.Grade,
            Status = e.Status.ToString(),
            Roles = e.EmployeeRoles.Select(er => er.Role.Name).ToList()
        }).ToList();

        return PaginationService.CreateResponse(
            results, pagination.PageNumber, pagination.PageSize, totalCount);
    }

    public async Task<EmployeeProfileDto> AssignRolesAsync(
        int employeeId,
        List<string> roleNames,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var names = (roleNames ?? new List<string>())
            .Select(r => r.Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            throw new ArgumentException("At least one role must be assigned.");
        }

        var employee = await _context.Employees
            .Include(e => e.EmployeeRoles)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with ID {employeeId} was not found.");

        var roles = await _context.Roles
            .Where(r => names.Contains(r.Name))
            .ToListAsync(cancellationToken);

        if (roles.Count != names.Count)
        {
            throw new ArgumentException("One or more requested roles do not exist.");
        }

        // An admin cannot remove their own Admin role (would lock everyone out).
        if (employeeId == actorEmployeeId &&
            !roles.Any(r => r.Name == "Admin"))
        {
            throw new InvalidOperationException(
                "You cannot remove your own Admin role.");
        }

        _context.EmployeeRoles.RemoveRange(employee.EmployeeRoles);

        foreach (var role in roles)
        {
            _context.EmployeeRoles.Add(new EmployeeRoles
            {
                EmployeeId = employee.Id,
                RoleId = role.Id
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            ActorEmployeeId = actorEmployeeId,
            Action = "EmployeeRolesChanged",
            EntityType = "Employee",
            EntityId = employee.Id.ToString(),
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { Roles = names }),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return await GetEmployeeByIdAsync(employeeId, cancellationToken);
    }

    public async Task ResendSetupLinkAsync(
        int employeeId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with ID {employeeId} was not found.");

        if (employee.Status != EmployeeStatus.PendingActivation)
        {
            throw new InvalidOperationException(
                "A setup link can only be resent to an account that is not yet activated.");
        }

        var now = DateTime.UtcNow;

        // Resending invalidates every earlier unused token.
        var oldTokens = await _context.EmployeeSetupTokens
            .Where(t => t.EmployeeId == employeeId && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in oldTokens)
        {
            token.UsedAtUtc = now;
        }

        AddSetupTokenAndEmail(employee, now);

        _context.AuditLogs.Add(new AuditLog
        {
            ActorEmployeeId = actorEmployeeId,
            Action = "SetupLinkResent",
            EntityType = "Employee",
            EntityId = employee.Id.ToString(),
            CreatedAtUtc = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void AddSetupTokenAndEmail(Employee employee, DateTime now)
    {
        var rawToken = GenerateSecureToken();
        var expiryHours = _configuration.GetValue("App:SetupTokenExpiryHours", 48);

        _context.EmployeeSetupTokens.Add(new EmployeeSetupToken
        {
            EmployeeId = employee.Id,
            TokenHash = HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(expiryHours)
        });

        var baseUrl = _configuration["App:FrontendBaseUrl"];
        var link = string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/activate?token={Uri.EscapeDataString(rawToken)}";

        // Queued in the same transaction as the account itself.
        _context.EmailQueueItems.Add(EmailTemplates.ToQueueItem(
            employee.Email,
            "Activate Your Employee Self-Service Account",
            EmailTemplates.AccountSetup(employee.FirstName, rawToken, link, expiryHours),
            now));
    }

    /// <summary>
    /// True when making newManagerId the manager of employeeId would put
    /// employeeId somewhere above itself in the reporting chain.
    /// </summary>
    private async Task<bool> CreatesReportingCycleAsync(
        int employeeId,
        int newManagerId,
        CancellationToken cancellationToken)
    {
        var current = (int?)newManagerId;
        var visited = new HashSet<int>();

        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == employeeId)
            {
                return true;
            }

            var id = current.Value;

            current = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => e.ReportingManagerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    private static EmployeeProfileDto MapProfile(Employee employee)
    {
        return new EmployeeProfileDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Department = employee.Department,
            Grade = employee.Grade,
            PhoneNumber = employee.PhoneNumber,
            ReportingManagerId = employee.ReportingManagerId,
            ReportingManagerName = employee.ReportingManager is null
                ? null
                : $"{employee.ReportingManager.FirstName} {employee.ReportingManager.LastName}",
            Roles = employee.EmployeeRoles
                .Select(er => er.Role.Name)
                .ToList(),
            Status = employee.Status,
            CreatedAtUtc = employee.CreatedAtUtc,
            ActivatedAtUtc = employee.ActivatedAtUtc
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
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
