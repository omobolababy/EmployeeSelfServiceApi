using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.KPI;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.DTOs.KPI;
using EmployeeServiceApi.DTOs.KPIDto;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.KPI;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

/// <summary>
/// KPI workflow: HR creates a Draft plan, edits targets/weights while Draft and
/// activates it (weights must total exactly 100.00). The employee records
/// actual results and evidence comments while Active or Returned (until the
/// end of the effective deadline day, Africa/Lagos) and submits. The assigned
/// reviewer approves (which finalizes and locks the plan with a server-calculated
/// score) or returns it with a comment. HR can reopen an Active or Returned plan.
/// </summary>
public class KpiService : IKpiService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KpiService> _logger;

    public KpiService(
        ApplicationDbContext context,
        ILogger<KpiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // HR: create / edit / activate
    // ------------------------------------------------------------------

    public async Task<KpiPlanResponseDto> CreatePlanAsync(
        CreateKpiPlanDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
        await EnsureRoleAsync(actorEmployeeId, cancellationToken, "HR");

        // Another HR user must prepare an HR user's own plan.
        if (request.EmployeeId == actorEmployeeId)
            throw new UnauthorizedAccessException(
                "You cannot create a KPI plan for yourself. Another HR user must do it.");

        var owner = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException("The employee was not found.");

        if (owner.Status != EmployeeStatus.Active)
            throw new InvalidOperationException("The employee account is not active.");

        var period = await _context.KpiReviewPeriods
            .FirstOrDefaultAsync(p => p.Id == request.ReviewPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException("Review period was not found.");

        if (!period.IsOpen)
            throw new InvalidOperationException("This review period is closed.");

        if (await _context.KpiPlans.AnyAsync(
                p => p.EmployeeId == request.EmployeeId &&
                     p.ReviewPeriodId == request.ReviewPeriodId,
                cancellationToken))
            throw new InvalidOperationException(
                "A KPI plan already exists for this employee and review period.");

        ValidateTargets(request.Targets);

        var now = DateTime.UtcNow;

        var plan = new KpiPlan
        {
            EmployeeId = owner.Id,
            ReviewPeriodId = period.Id,
            Status = KpiPlanStatus.Draft,
            SubmissionDeadline = period.SubmissionDeadline,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Targets = request.Targets.Select(t => NewTarget(t, now)).ToList()
        };

        plan.ReviewHistory.Add(new KpiReviewHistory
        {
            ActorEmployeeId = actorEmployeeId,
            Action = "Created",
            PreviousStatus = KpiPlanStatus.Draft,
            NewStatus = plan.Status,
            CreatedAtUtc = now
        });

        _context.KpiPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    public async Task<KpiPlanResponseDto> UpdatePlanAsync(
        int planId,
        UpdateKpiPlanDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
        await EnsureRoleAsync(actorEmployeeId, cancellationToken, "HR");

        var plan = await _context.KpiPlans
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        if (plan.EmployeeId == actorEmployeeId)
            throw new UnauthorizedAccessException(
                "You cannot edit your own KPI plan. Another HR user must do it.");

        // Targets and weights are editable only while the plan is Draft.
        if (plan.Status != KpiPlanStatus.Draft)
            throw new InvalidOperationException(
                "Targets and weights can only be edited while the plan is a draft.");

        ValidateTargets(request.Targets);

        var now = DateTime.UtcNow;

        _context.KpiTargets.RemoveRange(plan.Targets);
        plan.Targets = request.Targets.Select(t => NewTarget(t, now)).ToList();
        plan.UpdatedAtUtc = now;

        plan.ReviewHistory.Add(new KpiReviewHistory
        {
            ActorEmployeeId = actorEmployeeId,
            Action = "TargetsEdited",
            PreviousStatus = plan.Status,
            NewStatus = plan.Status,
            CreatedAtUtc = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    public async Task<KpiPlanResponseDto> ActivatePlanAsync(
        int planId,
        ActivateKpiPlanDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
        await EnsureRoleAsync(actorEmployeeId, cancellationToken, "HR");

        var plan = await _context.KpiPlans
            .Include(p => p.Employee)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        if (plan.EmployeeId == actorEmployeeId)
            throw new UnauthorizedAccessException(
                "You cannot activate your own KPI plan. Another HR user must do it.");

        if (plan.Status != KpiPlanStatus.Draft)
            throw new InvalidOperationException("Only draft plans can be activated.");

        // Weights must total exactly 100.00 (no tolerance).
        ValidateWeights(plan.Targets.Select(t => t.WeightPercentage));

        var reviewerId = request.AssignedReviewerId ?? plan.Employee.ReportingManagerId;

        if (!reviewerId.HasValue)
            throw new ArgumentException(
                "Choose a reviewer: the employee has no reporting manager.");

        if (reviewerId.Value == plan.EmployeeId)
            throw new ArgumentException("The reviewer cannot be the plan owner.");

        var reviewer = await _context.Employees
            .Include(e => e.EmployeeRoles).ThenInclude(er => er.Role)
            .FirstOrDefaultAsync(e => e.Id == reviewerId.Value, cancellationToken);

        if (reviewer is null || reviewer.Status != EmployeeStatus.Active)
            throw new ArgumentException("The reviewer must be an active employee.");

        if (!reviewer.EmployeeRoles.Any(er => er.Role.Name == "Manager"))
            throw new ArgumentException("The reviewer must have the Manager role.");

        var now = DateTime.UtcNow;

        plan.AssignedReviewerId = reviewer.Id;
        plan.Status = KpiPlanStatus.Active;
        plan.ActivatedAtUtc = now;
        plan.UpdatedAtUtc = now;

        plan.ReviewHistory.Add(new KpiReviewHistory
        {
            ActorEmployeeId = actorEmployeeId,
            Action = "Activated",
            PreviousStatus = KpiPlanStatus.Draft,
            NewStatus = plan.Status,
            CreatedAtUtc = now
        });

        // Same transaction as the business change.
        NotificationHelper.Add(
            _context, plan.Employee, NotificationType.Kpi,
            "New KPI plan assigned",
            $"A KPI plan for {plan.ReviewPeriod.Name} has been assigned to you. " +
            $"Submit your results by {plan.SubmissionDeadline:yyyy-MM-dd}.",
            "KpiPlan", plan.Id);

        NotificationHelper.Add(
            _context, reviewer, NotificationType.Kpi,
            "You are the KPI reviewer",
            $"You are the assigned reviewer for {plan.Employee.FirstName} {plan.Employee.LastName}'s " +
            $"KPI plan ({plan.ReviewPeriod.Name}).",
            "KpiPlan", plan.Id);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Employee: record results, submit
    // ------------------------------------------------------------------

    public async Task<KpiPlanResponseDto> UpdateProgressAsync(
        int planId,
        UpdateKpiProgressDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.KpiPlans
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        if (plan.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You can only update your own KPI plan.");

        EnsureEditable(plan);

        var byId = plan.Targets.ToDictionary(t => t.Id);
        var now = DateTime.UtcNow;

        foreach (var item in request.Targets)
        {
            if (!byId.TryGetValue(item.TargetId, out var target))
                throw new ArgumentException($"Target {item.TargetId} does not belong to this plan.");

            if (item.ActualValue < 0)
                throw new ArgumentException("Actual values cannot be negative.");

            target.ActualValue = item.ActualValue;
            target.EmployeeComment = item.EvidenceComment?.Trim();
            target.UpdatedAtUtc = now;
        }

        plan.UpdatedAtUtc = now;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    public async Task<KpiPlanResponseDto> SubmitPlanAsync(
        int planId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.KpiPlans
            .Include(p => p.Employee)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        if (plan.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You can only submit your own KPI plan.");

        EnsureEditable(plan);

        if (plan.Targets.Any(t => !t.ActualValue.HasValue))
            throw new ArgumentException(
                "Enter an actual value for every KPI target before submitting.");

        var previous = plan.Status;
        var now = DateTime.UtcNow;

        // Submitting locks the plan until the reviewer returns it.
        plan.Status = KpiPlanStatus.Submitted;
        plan.SubmittedAtUtc = now;
        plan.UpdatedAtUtc = now;

        plan.ReviewHistory.Add(new KpiReviewHistory
        {
            ActorEmployeeId = employeeId,
            Action = "Submitted",
            PreviousStatus = previous,
            NewStatus = plan.Status,
            CreatedAtUtc = now
        });

        if (plan.AssignedReviewer is not null)
        {
            NotificationHelper.Add(
                _context, plan.AssignedReviewer, NotificationType.Kpi,
                "KPI plan awaiting your review",
                $"{plan.Employee.FirstName} {plan.Employee.LastName} submitted their KPI plan " +
                $"({plan.ReviewPeriod.Name}) for review.",
                "KpiPlan", plan.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Reviewer: approve (finalizes) or return
    // ------------------------------------------------------------------

    public async Task<KpiPlanResponseDto> ReviewPlanAsync(
        int planId,
        ReviewKpiPlanDto request,
        int reviewerEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(reviewerEmployeeId, cancellationToken);

        var plan = await _context.KpiPlans
            .Include(p => p.Employee)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        // Access is by STORED assignment, and never for your own plan.
        if (plan.AssignedReviewerId != reviewerEmployeeId ||
            plan.EmployeeId == reviewerEmployeeId)
            throw new UnauthorizedAccessException("You are not the assigned reviewer.");

        if (plan.Status != KpiPlanStatus.Submitted)
            throw new InvalidOperationException("Only submitted plans can be reviewed.");

        if (request.Decision is not KpiReviewDecision.Approve
            and not KpiReviewDecision.ReturnToEmployee)
            throw new ArgumentException("Select a valid review decision.");

        if (request.Decision == KpiReviewDecision.ReturnToEmployee &&
            string.IsNullOrWhiteSpace(request.ReviewerComment))
            throw new ArgumentException("A comment is required when returning a KPI plan.");

        var now = DateTime.UtcNow;
        var byId = plan.Targets.ToDictionary(t => t.Id);

        foreach (var item in request.Targets)
        {
            if (!byId.TryGetValue(item.TargetId, out var target))
                throw new ArgumentException($"Target {item.TargetId} does not belong to this plan.");

            target.ReviewerComment = item.ReviewerComment?.Trim();
            target.UpdatedAtUtc = now;
        }

        var previous = plan.Status;
        plan.ReviewerComment = request.ReviewerComment?.Trim();
        plan.UpdatedAtUtc = now;

        string action;
        string message;

        if (request.Decision == KpiReviewDecision.Approve)
        {
            ApplyScores(plan);

            plan.Status = KpiPlanStatus.Finalized;
            plan.FinalizedAtUtc = now;
            action = "Finalized";
            message = $"Your KPI plan ({plan.ReviewPeriod.Name}) was finalized with an overall score of " +
                      $"{plan.FinalScorePercentage:0.00}.";
        }
        else
        {
            plan.Status = KpiPlanStatus.Returned;
            action = "ReturnedToEmployee";
            message = $"Your KPI plan ({plan.ReviewPeriod.Name}) was returned with feedback. " +
                      "Update your results and submit again.";
        }

        plan.ReviewHistory.Add(new KpiReviewHistory
        {
            ActorEmployeeId = reviewerEmployeeId,
            Action = action,
            Comment = plan.ReviewerComment,
            PreviousStatus = previous,
            NewStatus = plan.Status,
            CreatedAtUtc = now
        });

        NotificationHelper.Add(
            _context, plan.Employee, NotificationType.Kpi,
            request.Decision == KpiReviewDecision.Approve ? "KPI plan finalized" : "KPI plan returned",
            message, "KpiPlan", plan.Id);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    // ------------------------------------------------------------------
    // HR: reopen
    // ------------------------------------------------------------------

    public async Task<KpiPlanResponseDto> ReopenPlanAsync(
        int planId,
        ReopenKpiPlanDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
        await EnsureRoleAsync(actorEmployeeId, cancellationToken, "HR");

        var plan = await _context.KpiPlans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A reason is required to reopen a KPI plan.");

        var today = ReminderUtilities.GetLagosDate(DateTime.UtcNow);

        if (request.NewDeadline <= today)
            throw new ArgumentException("The new deadline must be in the future.");

        switch (plan.Status)
        {
            case KpiPlanStatus.Finalized:
                throw new InvalidOperationException("A finalized KPI plan cannot be reopened.");
            case KpiPlanStatus.Submitted:
                throw new InvalidOperationException(
                    "A submitted plan needs to be returned by its reviewer before HR can reopen it.");
            case KpiPlanStatus.Active:
            case KpiPlanStatus.Returned:
                break;
            default:
                throw new InvalidOperationException(
                    "Only active or returned plans can be reopened.");
        }

        var previousDeadline = plan.RevisedDeadline ?? plan.SubmissionDeadline;
        var now = DateTime.UtcNow;

        // SubmissionDeadline (the original) is never overwritten.
        plan.RevisedDeadline = request.NewDeadline;
        plan.UpdatedAtUtc = now;

        plan.ReopeningHistory.Add(new KpiReopeningHistory
        {
            ReopenedByEmployeeId = actorEmployeeId,
            Reason = request.Reason.Trim(),
            PreviousDeadline = previousDeadline,
            NewDeadline = request.NewDeadline,
            ReopenedAtUtc = now
        });

        plan.ReviewHistory.Add(new KpiReviewHistory
        {
            ActorEmployeeId = actorEmployeeId,
            Action = "Reopened",
            Comment = request.Reason.Trim(),
            PreviousStatus = plan.Status,
            NewStatus = plan.Status,
            CreatedAtUtc = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return await GetPlanForMappingAsync(plan.Id, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    public async Task<KpiPlanResponseDto> GetPlanAsync(
        int planId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);

        var plan = await _context.KpiPlans
            .AsNoTracking()
            .Include(p => p.Employee)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.Targets)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new KeyNotFoundException("KPI plan was not found.");

        var permitted = plan.EmployeeId == actorEmployeeId ||
                        plan.AssignedReviewerId == actorEmployeeId ||
                        await HasAnyRoleAsync(actorEmployeeId, cancellationToken, "HR", "Admin");

        if (!permitted)
            throw new UnauthorizedAccessException(
                "You do not have permission to view this KPI plan.");

        // Draft plans are not shown to the employee until activated.
        if (plan.EmployeeId == actorEmployeeId &&
            plan.Status == KpiPlanStatus.Draft &&
            !await HasAnyRoleAsync(actorEmployeeId, cancellationToken, "HR", "Admin"))
            throw new KeyNotFoundException("KPI plan was not found.");

        return MapPlan(plan);
    }

    public async Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<KpiPlanResponseDto>> GetAllPlansAsync(
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
        await EnsureRoleAsync(actorEmployeeId, cancellationToken, "HR", "Admin");

        return await GetPlansPageAsync(p => true, pagination, cancellationToken);
    }

    public async Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<KpiPlanResponseDto>> GetMyPlansAsync(
        int employeeId,
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(employeeId, cancellationToken);

        // Drafts are never visible to the employee.
        return await GetPlansPageAsync(
            p => p.EmployeeId == employeeId && p.Status != KpiPlanStatus.Draft,
            pagination, cancellationToken);
    }

    public async Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<KpiPlanResponseDto>> GetReviewQueueAsync(
        int reviewerEmployeeId,
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveEmployeeAsync(reviewerEmployeeId, cancellationToken);

        // Only plans ASSIGNED to this reviewer, never their own.
        return await GetPlansPageAsync(
            p => p.AssignedReviewerId == reviewerEmployeeId &&
                 p.EmployeeId != reviewerEmployeeId &&
                 p.Status == KpiPlanStatus.Submitted,
            pagination,
            cancellationToken);
    }

    public async Task<KpiReviewPeriodResponseDto> CreateReviewPeriodAsync(
        CreateReviewPeriodDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureActiveEmployeeAsync(actorEmployeeId, cancellationToken);
            await EnsureRoleAsync(
                actorEmployeeId, cancellationToken, "HR", "Admin");

            if (request.EndDate < request.StartDate)
                throw new ArgumentException(
                    "The end date must be on or after the start date.");

            if (request.SubmissionDeadline < request.StartDate ||
                request.SubmissionDeadline > request.EndDate)
                throw new ArgumentException(
                    "The submission deadline must fall within the review period.");

            var name = request.Name.Trim();

            if (await _context.KpiReviewPeriods.AnyAsync(
                    p => p.Name.ToLower() == name.ToLower(),
                    cancellationToken))
                throw new InvalidOperationException(
                    "A review period with this name already exists.");

            var now = DateTime.UtcNow;

            var period = new KpiReviewPeriod
            {
                Name = name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                SubmissionDeadline = request.SubmissionDeadline,
                IsOpen = request.IsOpen,
                CreatedAtUtc = now
            };

            _context.KpiReviewPeriods.Add(period);
            await _context.SaveChangesAsync(cancellationToken);

            _context.AuditLogs.Add(new AuditLog
            {
                ActorEmployeeId = actorEmployeeId,
                Action = "KpiReviewPeriodCreated",
                EntityType = nameof(KpiReviewPeriod),
                EntityId = period.Id.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            return new KpiReviewPeriodResponseDto
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                SubmissionDeadline = period.SubmissionDeadline,
                IsOpen = period.IsOpen,
                CreatedAtUtc = period.CreatedAtUtc
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create KPI review period.");
            throw;
        }
    }

    // ---------------- Helpers ----------------

    private static KpiTarget NewTarget(KpiTargetDto t, DateTime now) => new()
    {
        Title = t.Title.Trim(),
        Description = t.Description.Trim(),
        TargetValue = t.TargetValue,
        Unit = t.Unit.Trim(),
        WeightPercentage = t.WeightPercentage,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static void ValidateTargets(List<KpiTargetDto> targets)
    {
        if (targets is null || targets.Count == 0)
            throw new ArgumentException("At least one KPI target is required.");

        foreach (var t in targets)
        {
            if (string.IsNullOrWhiteSpace(t.Title) || string.IsNullOrWhiteSpace(t.Unit))
                throw new ArgumentException("Every KPI target needs a title and a unit.");

            if (t.TargetValue <= 0)
                throw new ArgumentException("A KPI target value must be greater than zero.");

            if (t.WeightPercentage <= 0 || t.WeightPercentage > 100)
                throw new ArgumentException(
                    "Each target weight must be greater than zero and no more than 100.");
        }
    }

    private static void ValidateWeights(IEnumerable<decimal> weights)
    {
        var values = weights.ToList();

        if (values.Count == 0)
            throw new ArgumentException("At least one KPI target is required.");

        if (values.Sum() != 100.00m)
            throw new ArgumentException("KPI target weights must total exactly 100.00.");
    }

    /// <summary>Owner edits/submissions: Active or Returned, up to the end of the deadline day (Africa/Lagos).</summary>
    private static void EnsureEditable(KpiPlan plan)
    {
        if (plan.Status is not KpiPlanStatus.Active and not KpiPlanStatus.Returned)
            throw new InvalidOperationException(
                "Results can only be changed while the plan is active or returned.");

        var today = ReminderUtilities.GetLagosDate(DateTime.UtcNow);
        var deadline = plan.RevisedDeadline ?? plan.SubmissionDeadline;

        // The deadline runs to the end of that calendar day.
        if (today > deadline)
            throw new InvalidOperationException(
                $"The submission deadline ({deadline:yyyy-MM-dd}) has passed.");
    }

    /// <summary>
    /// achievement % = min(actual / target x 100, 100)
    /// weighted score = achievement / 100 x weight
    /// overall = sum of weighted scores, rounded to 2 dp (AwayFromZero).
    /// </summary>
    public static decimal CalculateOverallScore(
        IEnumerable<(decimal Target, decimal Actual, decimal Weight)> items)
    {
        var total = items.Sum(i =>
            Achievement(i.Actual, i.Target) / 100m * i.Weight);

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal Achievement(decimal actual, decimal target) =>
        Math.Min(actual / target * 100m, 100m);

    private static void ApplyScores(KpiPlan plan)
    {
        foreach (var t in plan.Targets)
        {
            if (!t.ActualValue.HasValue || t.TargetValue <= 0)
                throw new InvalidOperationException(
                    "Every KPI target needs an actual value before scoring.");

            t.ScorePercentage = Math.Round(
                Achievement(t.ActualValue.Value, t.TargetValue),
                2, MidpointRounding.AwayFromZero);
        }

        plan.FinalScorePercentage = CalculateOverallScore(
            plan.Targets.Select(t => (t.TargetValue, t.ActualValue!.Value, t.WeightPercentage)));
    }

    private async Task EnsureActiveEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException("Employee was not found.");

        if (employee.Status != EmployeeStatus.Active)
            throw new UnauthorizedAccessException("Your employee account is not active.");
    }

    private async Task EnsureRoleAsync(
        int employeeId,
        CancellationToken cancellationToken,
        params string[] roles)
    {
        if (!await HasAnyRoleAsync(employeeId, cancellationToken, roles))
            throw new UnauthorizedAccessException(
                "You do not have permission to perform this action.");
    }

    private async Task<bool> HasAnyRoleAsync(
        int employeeId,
        CancellationToken cancellationToken,
        params string[] roles)
    {
        return await _context.EmployeeRoles.AnyAsync(
            er => er.EmployeeId == employeeId &&
                  roles.Contains(er.Role.Name),
            cancellationToken);
    }

    private async Task<EmployeeServiceApi.DTOs.Common.PagedResponseDto<KpiPlanResponseDto>> GetPlansPageAsync(
        System.Linq.Expressions.Expression<Func<KpiPlan, bool>> filter,
        EmployeeServiceApi.DTOs.Common.PaginationRequestDto pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.KpiPlans.AsNoTracking().Where(filter);
        var total = await query.CountAsync(cancellationToken);

        var plans = await query
            .Include(p => p.Employee)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.Targets)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            plans.Select(MapPlan).ToList(),
            pagination.PageNumber,
            pagination.PageSize,
            total);
    }

    private async Task<KpiPlanResponseDto> GetPlanForMappingAsync(
        int planId,
        CancellationToken cancellationToken)
    {
        var plan = await _context.KpiPlans
            .AsNoTracking()
            .Include(p => p.Employee)
            .Include(p => p.ReviewPeriod)
            .Include(p => p.AssignedReviewer)
            .Include(p => p.Targets)
            .FirstAsync(p => p.Id == planId, cancellationToken);

        return MapPlan(plan);
    }

    private static KpiPlanResponseDto MapPlan(KpiPlan plan)
    {
        return new KpiPlanResponseDto
        {
            Id = plan.Id,
            EmployeeId = plan.EmployeeId,
            EmployeeName = $"{plan.Employee.FirstName} {plan.Employee.LastName}",
            ReviewPeriodId = plan.ReviewPeriodId,
            ReviewPeriodName = plan.ReviewPeriod.Name,
            AssignedReviewerId = plan.AssignedReviewerId,
            AssignedReviewerName = plan.AssignedReviewer is null
                ? null
                : $"{plan.AssignedReviewer.FirstName} {plan.AssignedReviewer.LastName}",
            Status = plan.Status,
            SubmissionDeadline = plan.SubmissionDeadline,
            RevisedDeadline = plan.RevisedDeadline,
            EffectiveDeadline = plan.RevisedDeadline ?? plan.SubmissionDeadline,
            FinalScorePercentage = plan.FinalScorePercentage,
            ReviewerComment = plan.ReviewerComment,
            CreatedAtUtc = plan.CreatedAtUtc,
            UpdatedAtUtc = plan.UpdatedAtUtc,
            ActivatedAtUtc = plan.ActivatedAtUtc,
            SubmittedAtUtc = plan.SubmittedAtUtc,
            FinalizedAtUtc = plan.FinalizedAtUtc,
            Targets = plan.Targets.OrderBy(t => t.Id).Select(t => new KpiTargetResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                TargetValue = t.TargetValue,
                Unit = t.Unit,
                WeightPercentage = t.WeightPercentage,
                ActualValue = t.ActualValue,
                ScorePercentage = t.ScorePercentage,
                WeightedScore = t.ScorePercentage.HasValue
                    ? Math.Round(t.ScorePercentage.Value / 100m * t.WeightPercentage,
                        2, MidpointRounding.AwayFromZero)
                    : null,
                EmployeeComment = t.EmployeeComment,
                ReviewerComment = t.ReviewerComment
            }).ToList()
        };
    }
}
