using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Opportunities;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.JobApplication;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OpportunityService> _logger;

    public OpportunityService(
        ApplicationDbContext context,
        ILogger<OpportunityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // HR: create / edit / publish / close
    // ------------------------------------------------------------------

    public async Task<OpportunityResponseDto> CreateOpportunityAsync(
        CreateOpportunityDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHrAsync(actorEmployeeId, cancellationToken);

        var now = DateTime.UtcNow;

        var opportunity = new Opportunity
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Requirements = request.Requirements?.Trim() ?? string.Empty,
            Type = request.Type,
            Status = OpportunityStatus.Draft,
            OpensAtUtc = AsUtc(request.OpensAtUtc),
            ClosesAtUtc = AsUtc(request.ClosesAtUtc),
            CreatedByEmployeeId = actorEmployeeId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        ValidateWindow(opportunity.OpensAtUtc, opportunity.ClosesAtUtc);
        SetEligibility(opportunity, request.EligibleDepartments, request.EligibleGrades);

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetOpportunityByIdAsync(opportunity.Id, actorEmployeeId, cancellationToken);
    }

    public async Task<OpportunityResponseDto> UpdateOpportunityAsync(
        int opportunityId,
        UpdateOpportunityDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHrAsync(actorEmployeeId, cancellationToken);

        var opportunity = await _context.Opportunities
            .Include(o => o.EligibleDepartments)
            .Include(o => o.EligibleGrades)
            .FirstOrDefaultAsync(o => o.Id == opportunityId, cancellationToken)
            ?? throw new KeyNotFoundException("Opportunity was not found.");

        if (opportunity.Status == OpportunityStatus.Closed)
        {
            throw new InvalidOperationException(
                "A closed opportunity cannot be edited. Create a new one instead.");
        }

        opportunity.Title = request.Title.Trim();
        opportunity.Description = request.Description.Trim();
        opportunity.Requirements = request.Requirements?.Trim() ?? string.Empty;
        opportunity.OpensAtUtc = AsUtc(request.OpensAtUtc);
        opportunity.ClosesAtUtc = AsUtc(request.ClosesAtUtc);
        opportunity.UpdatedAtUtc = DateTime.UtcNow;

        ValidateWindow(opportunity.OpensAtUtc, opportunity.ClosesAtUtc);

        if (opportunity.Status == OpportunityStatus.Published &&
            (opportunity.ClosesAtUtc is null || opportunity.ClosesAtUtc <= DateTime.UtcNow))
        {
            throw new ArgumentException(
                "A published opportunity must have a closing time in the future.");
        }

        _context.OpportunityDepartmentEligibilities.RemoveRange(opportunity.EligibleDepartments);
        _context.OpportunityGradeEligibilities.RemoveRange(opportunity.EligibleGrades);
        opportunity.EligibleDepartments.Clear();
        opportunity.EligibleGrades.Clear();

        SetEligibility(opportunity, request.EligibleDepartments, request.EligibleGrades);

        // Existing applications keep their own snapshot and are untouched.
        await _context.SaveChangesAsync(cancellationToken);

        return await GetOpportunityByIdAsync(opportunityId, actorEmployeeId, cancellationToken);
    }

    public async Task<OpportunityResponseDto> PublishOpportunityAsync(
        int opportunityId,
        PublishOpportunityDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHrAsync(actorEmployeeId, cancellationToken);

        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.Id == opportunityId, cancellationToken)
            ?? throw new KeyNotFoundException("Opportunity was not found.");

        if (opportunity.Status != OpportunityStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only draft opportunities can be published.");
        }

        if (opportunity.ClosesAtUtc is null || opportunity.ClosesAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException(
                "Publishing requires a closing time in the future.");
        }

        opportunity.Status = OpportunityStatus.Published;
        opportunity.OpensAtUtc ??= DateTime.UtcNow;
        opportunity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetOpportunityByIdAsync(opportunityId, actorEmployeeId, cancellationToken);
    }

    public async Task<OpportunityResponseDto> CloseOpportunityAsync(
        int opportunityId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHrAsync(actorEmployeeId, cancellationToken);

        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.Id == opportunityId, cancellationToken)
            ?? throw new KeyNotFoundException("Opportunity was not found.");

        if (opportunity.Status != OpportunityStatus.Published)
        {
            throw new InvalidOperationException(
                "Only published opportunities can be closed.");
        }

        // A closed opportunity can never be reopened.
        opportunity.Status = OpportunityStatus.Closed;
        opportunity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetOpportunityByIdAsync(opportunityId, actorEmployeeId, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    public async Task<OpportunityResponseDto> GetOpportunityByIdAsync(
        int opportunityId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var isHr = await HasRoleAsync(actorEmployeeId, "HR", cancellationToken);

        var opportunity = await _context.Opportunities
            .AsNoTracking()
            .Include(o => o.EligibleDepartments)
            .Include(o => o.EligibleGrades)
            .FirstOrDefaultAsync(o => o.Id == opportunityId, cancellationToken);

        // Staff never learn that a draft exists.
        if (opportunity is null ||
            (!isHr && opportunity.Status != OpportunityStatus.Published))
        {
            throw new KeyNotFoundException("Opportunity was not found.");
        }

        return await MapOpportunityAsync(opportunity, cancellationToken);
    }

    public async Task<PagedResponseDto<OpportunityResponseDto>> GetOpenOpportunitiesAsync(
        PaginationRequestDto pagination,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var isHr = await HasRoleAsync(employeeId, "HR", cancellationToken);
        var now = DateTime.UtcNow;

        var query = _context.Opportunities.AsNoTracking();

        // HR sees every record; everyone else sees published, still-open ones.
        if (!isHr)
        {
            query = query.Where(o =>
                o.Status == OpportunityStatus.Published &&
                o.ClosesAtUtc != null && o.ClosesAtUtc > now);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(o => o.EligibleDepartments)
            .Include(o => o.EligibleGrades)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = new List<OpportunityResponseDto>();

        foreach (var item in items)
        {
            mapped.Add(await MapOpportunityAsync(item, cancellationToken));
        }

        return PaginationService.CreateResponse(
            mapped, pagination.PageNumber, pagination.PageSize, total);
    }

    // ------------------------------------------------------------------
    // Applications
    // ------------------------------------------------------------------

    public async Task<JobApplicationResponseDto> ApplyForOpportunityAsync(
        int opportunityId,
        ApplyForOpportunityDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Id == employeeId && e.Status == EmployeeStatus.Active,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("An active employee account is required.");

        var opportunity = await _context.Opportunities
            .Include(o => o.EligibleDepartments)
            .Include(o => o.EligibleGrades)
            .FirstOrDefaultAsync(o => o.Id == opportunityId, cancellationToken);

        if (opportunity is null || opportunity.Status == OpportunityStatus.Draft)
        {
            throw new KeyNotFoundException("Opportunity was not found.");
        }

        // The closing timestamp is enforced even if HR never closed the record.
        if (opportunity.Status != OpportunityStatus.Published ||
            opportunity.ClosesAtUtc is null ||
            opportunity.ClosesAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "This opportunity is closed to applications.");
        }

        var departments = opportunity.EligibleDepartments.Select(d => d.Department).ToList();
        var grades = opportunity.EligibleGrades.Select(g => g.Grade).ToList();

        var departmentOk = departments.Count == 0 ||
            departments.Contains(employee.Department, StringComparer.OrdinalIgnoreCase);
        var gradeOk = grades.Count == 0 ||
            grades.Contains(employee.Grade, StringComparer.OrdinalIgnoreCase);

        if (!departmentOk || !gradeOk)
        {
            throw new UnauthorizedAccessException(
                "Your department or grade is not eligible for this opportunity.");
        }

        var duplicate = await _context.JobApplications.AnyAsync(
            a => a.EmployeeId == employeeId && a.OpportunityId == opportunityId,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "You have already applied for this opportunity.");
        }

        if (request.AttachmentId.HasValue)
        {
            var validCv = await _context.Attachments.AnyAsync(
                a => a.Id == request.AttachmentId.Value &&
                     a.UploadedByEmployeeId == employeeId &&
                     a.EntityType == AttachmentService.OpportunityType &&
                     a.EntityId == opportunityId &&
                     a.Category == "ApplicationCv",
                cancellationToken);

            if (!validCv)
            {
                throw new InvalidOperationException(
                    "The CV attachment is not valid for this application.");
            }
        }

        var now = DateTime.UtcNow;

        var application = new JobApplication
        {
            EmployeeId = employeeId,
            OpportunityId = opportunityId,
            Status = ApplicationStatus.Submitted,
            CoverLetter = request.CoverLetter?.Trim(),
            AttachmentId = request.AttachmentId,
            // Eligibility snapshot: later transfers never invalidate it.
            DepartmentSnapshot = employee.Department,
            GradeSnapshot = employee.Grade,
            EligibleAtSubmission = true,
            AppliedAtUtc = now,
            UpdatedAtUtc = now
        };

        application.History.Add(new JobApplicationHistory
        {
            ActorEmployeeId = employeeId,
            PreviousStatus = ApplicationStatus.Submitted,
            NewStatus = ApplicationStatus.Submitted,
            Comment = "Application submitted.",
            CreatedAtUtc = now
        });

        _context.JobApplications.Add(application);
        await _context.SaveChangesAsync(cancellationToken);

        return MapApplication(application, employee, opportunity);
    }

    public async Task<JobApplicationResponseDto> GetApplicationByIdAsync(
        int applicationId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken);

        if (application.EmployeeId != actorEmployeeId &&
            !await HasRoleAsync(actorEmployeeId, "HR", cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to view this application.");
        }

        return MapApplication(application, application.Employee, application.Opportunity);
    }

    public async Task<PagedResponseDto<JobApplicationResponseDto>> GetMyApplicationsAsync(
        int employeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobApplications
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(a => a.Employee)
            .Include(a => a.Opportunity)
            .OrderByDescending(a => a.AppliedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            items.Select(a => MapApplication(a, a.Employee, a.Opportunity)),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    public async Task<PagedResponseDto<JobApplicationResponseDto>> GetApplicantsAsync(
        int opportunityId,
        PaginationRequestDto pagination,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHrAsync(actorEmployeeId, cancellationToken);

        var exists = await _context.Opportunities.AnyAsync(
            o => o.Id == opportunityId, cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException("Opportunity was not found.");
        }

        var query = _context.JobApplications
            .AsNoTracking()
            .Where(a => a.OpportunityId == opportunityId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(a => a.Employee)
            .Include(a => a.Opportunity)
            .OrderBy(a => a.AppliedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            items.Select(a => MapApplication(a, a.Employee, a.Opportunity)),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    public async Task<JobApplicationResponseDto> UpdateApplicationStatusAsync(
        int applicationId,
        UpdateApplicationStatusDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureHrAsync(actorEmployeeId, cancellationToken);

        var application = await LoadApplicationAsync(applicationId, cancellationToken, track: true);

        if (application.EmployeeId == actorEmployeeId)
        {
            throw new UnauthorizedAccessException(
                "You cannot decide on your own application.");
        }

        var allowed = (application.Status, request.Status) switch
        {
            (ApplicationStatus.Submitted, ApplicationStatus.Shortlisted) => true,
            (ApplicationStatus.Submitted, ApplicationStatus.Rejected) => true,
            (ApplicationStatus.Shortlisted, ApplicationStatus.Selected) => true,
            (ApplicationStatus.Shortlisted, ApplicationStatus.Rejected) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"An application cannot move from {application.Status} to {request.Status}.");
        }

        var previous = application.Status;
        var now = DateTime.UtcNow;

        application.Status = request.Status;
        application.UpdatedAtUtc = now;

        application.History.Add(new JobApplicationHistory
        {
            ActorEmployeeId = actorEmployeeId,
            PreviousStatus = previous,
            NewStatus = request.Status,
            Comment = request.Comment?.Trim(),
            CreatedAtUtc = now
        });

        // Applicant is notified of every status change (same transaction).
        NotificationHelper.Add(
            _context,
            application.Employee,
            NotificationType.Application,
            "Application status update",
            $"Your application for \"{application.Opportunity.Title}\" is now {request.Status}.",
            "JobApplication",
            application.Id);

        await _context.SaveChangesAsync(cancellationToken);

        return MapApplication(application, application.Employee, application.Opportunity);
    }

    public async Task<JobApplicationResponseDto> WithdrawApplicationAsync(
        int applicationId,
        WithdrawApplicationDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadApplicationAsync(applicationId, cancellationToken, track: true);

        if (application.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "You can only withdraw your own application.");
        }

        if (application.Status is not (ApplicationStatus.Submitted or ApplicationStatus.Shortlisted))
        {
            throw new InvalidOperationException(
                $"A {application.Status} application cannot be withdrawn.");
        }

        var previous = application.Status;
        var now = DateTime.UtcNow;

        application.Status = ApplicationStatus.Withdrawn;
        application.WithdrawnAtUtc = now;
        application.UpdatedAtUtc = now;

        application.History.Add(new JobApplicationHistory
        {
            ActorEmployeeId = employeeId,
            PreviousStatus = previous,
            NewStatus = ApplicationStatus.Withdrawn,
            Comment = request.Reason?.Trim(),
            CreatedAtUtc = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapApplication(application, application.Employee, application.Opportunity);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<JobApplication> LoadApplicationAsync(
        int applicationId,
        CancellationToken cancellationToken,
        bool track = false)
    {
        var query = _context.JobApplications
            .Include(a => a.Employee)
            .Include(a => a.Opportunity)
            .Include(a => a.History)
            .AsQueryable();

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
               ?? throw new KeyNotFoundException("Application was not found.");
    }

    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue
            ? value.Value.Kind == DateTimeKind.Local
                ? value.Value.ToUniversalTime()
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;

    private static void ValidateWindow(DateTime? opensAt, DateTime? closesAt)
    {
        if (opensAt.HasValue && closesAt.HasValue && closesAt <= opensAt)
        {
            throw new ArgumentException(
                "The closing time must be after the opening time.");
        }
    }

    private static void SetEligibility(
        Opportunity opportunity,
        IEnumerable<string>? departments,
        IEnumerable<string>? grades)
    {
        foreach (var department in Clean(departments))
        {
            opportunity.EligibleDepartments.Add(
                new OpportunityDepartmentEligibility { Department = department });
        }

        foreach (var grade in Clean(grades))
        {
            opportunity.EligibleGrades.Add(
                new OpportunityGradeEligibility { Grade = grade });
        }
    }

    private static IEnumerable<string> Clean(IEnumerable<string>? values) =>
        (values ?? Enumerable.Empty<string>())
            .Select(v => v?.Trim() ?? string.Empty)
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private async Task<OpportunityResponseDto> MapOpportunityAsync(
        Opportunity o,
        CancellationToken cancellationToken)
    {
        var applicationCount = await _context.JobApplications.CountAsync(
            a => a.OpportunityId == o.Id, cancellationToken);

        return new OpportunityResponseDto
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            Requirements = o.Requirements,
            Type = o.Type,
            Status = o.Status,
            OpensAtUtc = o.OpensAtUtc,
            ClosesAtUtc = o.ClosesAtUtc,
            CreatedByEmployeeId = o.CreatedByEmployeeId,
            CreatedAtUtc = o.CreatedAtUtc,
            UpdatedAtUtc = o.UpdatedAtUtc,
            EligibleDepartments = o.EligibleDepartments.Select(d => d.Department).ToList(),
            EligibleGrades = o.EligibleGrades.Select(g => g.Grade).ToList(),
            ApplicationCount = applicationCount
        };
    }

    private static JobApplicationResponseDto MapApplication(
        JobApplication a,
        Employee employee,
        Opportunity opportunity) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        EmployeeName = $"{employee.FirstName} {employee.LastName}",
        OpportunityId = a.OpportunityId,
        OpportunityTitle = opportunity.Title,
        OpportunityType = opportunity.Type,
        Status = a.Status,
        CoverLetter = a.CoverLetter,
        AttachmentId = a.AttachmentId,
        DepartmentSnapshot = a.DepartmentSnapshot,
        GradeSnapshot = a.GradeSnapshot,
        EligibleAtSubmission = a.EligibleAtSubmission,
        AppliedAtUtc = a.AppliedAtUtc,
        UpdatedAtUtc = a.UpdatedAtUtc,
        WithdrawnAtUtc = a.WithdrawnAtUtc
    };

    private async Task<bool> HasRoleAsync(
        int employeeId,
        string role,
        CancellationToken cancellationToken) =>
        await _context.EmployeeRoles.AnyAsync(
            er => er.EmployeeId == employeeId && er.Role.Name == role,
            cancellationToken);

    private async Task EnsureHrAsync(int employeeId, CancellationToken cancellationToken)
    {
        var isActiveHr = await _context.Employees.AnyAsync(
            e => e.Id == employeeId &&
                 e.Status == EmployeeStatus.Active &&
                 e.EmployeeRoles.Any(er => er.Role.Name == "HR"),
            cancellationToken);

        if (!isActiveHr)
        {
            throw new UnauthorizedAccessException("Only HR users can perform this action.");
        }
    }
}
