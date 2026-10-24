using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Payroll;
using EmployeeSelfServiceApi.Entities;
using EmployeeSelfServiceApi.Exceptions;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.Payslip;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        ApplicationDbContext context,
        ILogger<PayrollService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PayslipResponseDto> CreatePayslipAsync(
        CreatePayslipDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureFinanceAsync(actorEmployeeId, cancellationToken);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.Id == request.EmployeeId,
                    cancellationToken);

            if (employee is null)
                throw new KeyNotFoundException("Employee was not found.");

            if (employee.Status != EmployeeStatus.Active)
                throw new InvalidOperationException(
                    "A payslip cannot be prepared for an inactive employee.");

            if (actorEmployeeId == employee.Id)
                throw new UnauthorizedAccessException(
                    "Finance users cannot prepare their own payslips.");

            ValidateFinancialDetails(request.BasicSalary, request.Items);

            var exists = await _context.Payslips.AnyAsync(
                p => p.EmployeeId == request.EmployeeId &&
                     p.PayrollMonth == request.PayrollMonth &&
                     p.PayrollYear == request.PayrollYear,
                cancellationToken);

            if (exists)
                throw new InvalidOperationException(
                    "A payslip already exists for this employee and payroll period.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var payslip = new Payslip
            {
                EmployeeId = employee.Id,
                PayrollMonth = request.PayrollMonth,
                PayrollYear = request.PayrollYear,
                CreatedAtUtc = now
            };

            var version = BuildVersion(
                payslip,
                versionNumber: 1,
                request.BasicSalary,
                request.Items,
                actorEmployeeId,
                revisionReason: null,
                now);

            payslip.Versions.Add(version);
            _context.Payslips.Add(payslip);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await LoadPayslipResponseAsync(
                payslip.Id, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex, "Database error preparing payslip for employee {EmployeeId}.",
                request.EmployeeId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Failed to prepare payslip for employee {EmployeeId}.",
                request.EmployeeId);
            throw;
        }
    }

    public async Task<PayslipVersionResponseDto> RevisePayslipAsync(
        int payslipId,
        RevisePayslipDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureFinanceAsync(actorEmployeeId, cancellationToken);

            var payslip = await _context.Payslips
                .Include(p => p.Employee)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Items)
                .FirstOrDefaultAsync(
                    p => p.Id == payslipId, cancellationToken);

            if (payslip is null)
                throw new ApiException(
                    StatusCodes.Status404NotFound,
                    "Payslip not found.");

            if (payslip.EmployeeId == actorEmployeeId)
                throw new UnauthorizedAccessException(
                    "Finance users cannot prepare their own payslips.");

            if (string.IsNullOrWhiteSpace(request.RevisionReason))
                throw new ArgumentException(
                    "A reason is required for a payslip revision.");

            var current = payslip.Versions
                .FirstOrDefault(v => v.Id == payslip.CurrentVersionId);

            if (current is null || current.Status != PayslipStatus.Published)
                throw new InvalidOperationException(
                    "Only a published payslip can be revised.");

            if (payslip.Versions.Any(v => v.Status == PayslipStatus.Draft))
                throw new InvalidOperationException(
                    "A draft revision already exists for this payslip.");

            ValidateFinancialDetails(request.BasicSalary, request.Items);

            var nextVersion = payslip.Versions.Max(v => v.VersionNumber) + 1;
            var now = DateTime.UtcNow;

            var revision = BuildVersion(
                payslip,
                nextVersion,
                request.BasicSalary,
                request.Items,
                actorEmployeeId,
                request.RevisionReason.Trim(),
                now);

            payslip.Versions.Add(revision);

            await _context.SaveChangesAsync(cancellationToken);

            return MapVersion(revision);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Failed to create revision for payslip {PayslipId}.",
                payslipId);
            throw;
        }
    }

    public async Task<PayslipVersionResponseDto> PublishPayslipAsync(
    int payslipId,
    int versionId,
    int actorEmployeeId,
    CancellationToken cancellationToken = default)
{
    await EnsureFinanceAsync(actorEmployeeId, cancellationToken);

    await using var transaction =
        await _context.Database.BeginTransactionAsync(cancellationToken);

    try
    {
        var payslip = await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Items)
            .FirstOrDefaultAsync(
                p => p.Id == payslipId,
                cancellationToken);

        if (payslip == null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "Payslip was not found.");
        }

        var version = payslip.Versions
            .FirstOrDefault(v => v.Id == versionId);

        if (version == null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "Payslip version was not found.");
        }

        if (version.Status != PayslipStatus.Draft)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Only a draft payslip version can be published.");
        }

        if (version.PreparedByEmployeeId == actorEmployeeId)
        {
            throw new ApiException(
                StatusCodes.Status403Forbidden,
                "You cannot publish a payslip you prepared.");
        }

        var previousVersion = payslip.Versions
            .FirstOrDefault(v => v.Id == payslip.CurrentVersionId);

        if (previousVersion != null &&
            previousVersion.Status != PayslipStatus.Published)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "The current payslip version is not in a publishable state.");
        }

        var now = DateTime.UtcNow;

        // Supersede the previously published version, if one exists.
        if (previousVersion != null)
        {
            previousVersion.Status = PayslipStatus.Superseded;
        }

        version.Status = PayslipStatus.Published;
        version.PublishedByEmployeeId = actorEmployeeId;
        version.PublishedAtUtc = now;

        payslip.CurrentVersionId = version.Id;

        _context.Notifications.Add(new Notification
        {
            EmployeeId = payslip.EmployeeId,
            Type = NotificationType.Published,
            Title = "Payslip published",
            Message =
                $"Your payslip for {payslip.PayrollMonth}/{payslip.PayrollYear} is now available.",
            RelatedEntityType = "Payslip",
            RelatedEntityId = payslip.Id,
            CreatedAtUtc = now
        });

        if (!string.IsNullOrWhiteSpace(payslip.Employee.Email))
        {
            _context.EmailQueueItems.Add(new EmailQueueItem
            {
                RecipientEmail = payslip.Employee.Email,
                Subject = "Your payslip is available",
                Body = EmailTemplates.Notification(
                    payslip.Employee.FirstName,
                    "Your payslip is available",
                    $"Your payslip for {payslip.PayrollMonth}/{payslip.PayrollYear} has been published. No attachment is included in this email."),
                Status = EmailMessageStatus.Pending,
                AttemptCount = 0,
                CreatedAtUtc = now,
                NextAttemptAtUtc = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapVersion(version);
    }
    catch (DbUpdateConcurrencyException exception)
    {
        await transaction.RollbackAsync(cancellationToken);

        _logger.LogWarning(
            exception,
            "Concurrent payslip update detected for payslip {PayslipId}",
            payslipId);

        throw new ApiException(
            StatusCodes.Status409Conflict,
            "This payslip was modified by another request. Refresh and try again.");
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
    public async Task<PayslipResponseDto> GetPayslipAsync(
        int payslipId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payslip = await _context.Payslips
                .Include(p => p.Employee)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Items)
                .FirstOrDefaultAsync(
                    p => p.Id == payslipId, cancellationToken);

            if (payslip is null)
                throw new KeyNotFoundException("Payslip was not found.");

            var isOwner = payslip.EmployeeId == actorEmployeeId;
            var isFinance = await HasRoleAsync(
                actorEmployeeId, "Finance", cancellationToken);

            if (!isOwner && !isFinance)
                throw new ApiException(
                    StatusCodes.Status403Forbidden,
                    "You cannot access this payslip.");

            // Owners can only see published/superseded versions.
            // Finance can see all versions including drafts.
            var versionsToReturn = isFinance 
                ? payslip.Versions 
                : payslip.Versions.Where(v => v.Status is PayslipStatus.Published or PayslipStatus.Superseded);

            return MapPayslip(payslip, versionsToReturn);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Failed to retrieve payslip {PayslipId}.", payslipId);
            throw;
        }
    }

    public async Task<List<PayslipResponseDto>> GetMyPayslipsAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payslips = await _context.Payslips
                .AsNoTracking()
                .Include(p => p.Employee)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Items)
                .Where(p => p.EmployeeId == employeeId &&
                    p.Versions.Any(v => v.Status == PayslipStatus.Published))
                .OrderByDescending(p => p.PayrollYear)
                .ThenByDescending(p => p.PayrollMonth)
                .ToListAsync(cancellationToken);

            return payslips.Select(p => MapPayslip(p, 
                p.Versions.Where(v => v.Status is PayslipStatus.Published or PayslipStatus.Superseded))).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Failed to list payslips for employee {EmployeeId}.",
                employeeId);
            throw;
        }
    }

    public async Task<PayslipResponseDto> GetPayslipForFinanceAsync(
        int payslipId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFinanceAsync(actorEmployeeId, cancellationToken);

        var payslip = await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.Versions)
            .ThenInclude(v => v.Items)
            .FirstOrDefaultAsync(
                p => p.Id == payslipId,
                cancellationToken);

        if (payslip == null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                "Payslip was not found.");
        }

        return MapPayslip(payslip, payslip.Versions);
    }

    public async Task<PayslipResponseDto> GetMyPayslipAsync(
        int payslipId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        return await GetPayslipAsync(payslipId, actorEmployeeId, cancellationToken);
    }

    public async Task<PayslipVersionResponseDto> GetPublishedVersionAsync(
        int payslipId,
        int versionId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payslip = await _context.Payslips
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Items)
                .FirstOrDefaultAsync(
                    p => p.Id == payslipId, cancellationToken);

            if (payslip is null)
                throw new KeyNotFoundException("Payslip was not found.");

            var isOwner = payslip.EmployeeId == actorEmployeeId;
            var isFinance = await HasRoleAsync(
                actorEmployeeId, "Finance", cancellationToken);

            if (!isOwner && !isFinance)
                throw new UnauthorizedAccessException(
                    "Only the payslip owner or Finance can access published versions.");

            var version = payslip.Versions.FirstOrDefault(
                v => v.Id == versionId);

            if (version is null ||
                version.Status is not PayslipStatus.Published
                    and not PayslipStatus.Superseded)
                throw new KeyNotFoundException(
                    "Published payslip version was not found.");

            return MapVersion(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Failed to retrieve version {VersionId} of payslip {PayslipId}.",
                versionId, payslipId);
            throw;
        }
    }

    // ---------------- Private helpers ----------------

    private PayslipVersion BuildVersion(
        Payslip payslip,
        int versionNumber,
        decimal basicSalary,
        IEnumerable<PayslipItemDto> items,
        int preparedByEmployeeId,
        string? revisionReason,
        DateTime now)
    {
        var inputItems = items.ToList();

        var allowances = inputItems
            .Where(i => i.Type == PayslipItemType.Allowance)
            .Sum(i => i.Amount);

        var deductions = inputItems
            .Where(i => i.Type == PayslipItemType.Deduction)
            .Sum(i => i.Amount);

        var gross = RoundMoney(basicSalary + allowances);
        var totalDeductions = RoundMoney(deductions);

        if (totalDeductions > gross)
            throw new ArgumentException(
                "Total deductions cannot exceed gross pay.");

        return new PayslipVersion
        {
            Payslip = payslip,
            VersionNumber = versionNumber,
            BasicSalary = RoundMoney(basicSalary),
            GrossPay = gross,
            TotalDeductions = totalDeductions,
            NetPay = RoundMoney(gross - totalDeductions),
            Currency = "NGN",
            Status = PayslipStatus.Draft,
            PreparedByEmployeeId = preparedByEmployeeId,
            RevisionReason = revisionReason,
            CreatedAtUtc = now,
            Items = inputItems.Select(i => new PayslipItem
            {
                Type = i.Type,
                Name = i.Name.Trim(),
                Amount = RoundMoney(i.Amount)
            }).ToList()
        };
    }

    private static void ValidateFinancialDetails(
        decimal basicSalary,
        IEnumerable<PayslipItemDto> items)
    {
        if (basicSalary < 0)
            throw new ArgumentException(
                "Basic salary cannot be negative.");

        foreach (var item in items)
        {
            if (item.Amount < 0)
                throw new ArgumentException(
                    "Payslip item amounts cannot be negative.");

            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException(
                    "Every payslip item must have a name.");

            if (item.Type is not PayslipItemType.Allowance
                and not PayslipItemType.Deduction)
                throw new ArgumentException(
                    "Payslip items must be allowances or deductions.");
        }
    }

    private async Task EnsureFinanceAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Id == employeeId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException("Employee was not found.");

        if (employee.Status != EmployeeStatus.Active)
            throw new UnauthorizedAccessException(
                "Your employee account is not active.");

        if (!await HasRoleAsync(employeeId, "Finance", cancellationToken))
            throw new UnauthorizedAccessException(
                "Only Finance users can perform this action.");
    }

    private Task<bool> HasRoleAsync(
        int employeeId,
        string role,
        CancellationToken cancellationToken)
    {
        return _context.EmployeeRoles.AnyAsync(
            er => er.EmployeeId == employeeId &&
                  er.Role.Name == role,
            cancellationToken);
    }

    private async Task<PayslipResponseDto> LoadPayslipResponseAsync(
        int payslipId,
        CancellationToken cancellationToken)
    {
        var payslip = await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Items)
            .FirstAsync(p => p.Id == payslipId, cancellationToken);

        return MapPayslip(payslip, payslip.Versions);
    }

    private static PayslipResponseDto MapPayslip(
        Payslip payslip,
        IEnumerable<PayslipVersion> versions)
    {
        var mappedVersions = versions
            .OrderBy(v => v.VersionNumber)
            .Select(MapVersion)
            .ToList();

        return new PayslipResponseDto
        {
            Id = payslip.Id,
            EmployeeId = payslip.EmployeeId,
            EmployeeName =
                $"{payslip.Employee.FirstName} {payslip.Employee.LastName}",
            EmployeeNumber = payslip.Employee.EmployeeNumber,
            PayrollMonth = payslip.PayrollMonth,
            PayrollYear = payslip.PayrollYear,
            CurrentVersionId = payslip.CurrentVersionId,
            CurrentVersion = mappedVersions.FirstOrDefault(
                v => v.Id == payslip.CurrentVersionId),
            Versions = mappedVersions
        };
    }

    private static PayslipVersionResponseDto MapVersion(
        PayslipVersion version)
    {
        return new PayslipVersionResponseDto
        {
            Id = version.Id,
            PayslipId = version.PayslipId,
            VersionNumber = version.VersionNumber,
            BasicSalary = version.BasicSalary,
            GrossPay = version.GrossPay,
            TotalDeductions = version.TotalDeductions,
            NetPay = version.NetPay,
            Currency = version.Currency,
            Status = version.Status,
            PreparedByEmployeeId = version.PreparedByEmployeeId,
            PublishedByEmployeeId = version.PublishedByEmployeeId,
            RevisionReason = version.RevisionReason,
            CreatedAtUtc = version.CreatedAtUtc,
            PublishedAtUtc = version.PublishedAtUtc,
            Items = version.Items.Select(i => new PayslipItemResponseDto
            {
                Id = i.Id,
                Type = i.Type,
                Name = i.Name,
                Amount = i.Amount
            }).ToList()
        };
    }

    private static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}