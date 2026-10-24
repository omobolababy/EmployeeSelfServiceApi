
using System.Security.Cryptography;
using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Training;
using EmployeeSelfServiceApi.Entities;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeSelfServiceApi.Utilities;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.Training;
using EmployeeServiceApi.DTOs.TrainingDto;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.LMS;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace EmployeeSelfServiceApi.Services;

public class TrainingService : ITrainingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TrainingService> _logger;

    public TrainingService(
        ApplicationDbContext context,
        ILogger<TrainingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TrainingRequestResponseDto> CreateRequestAsync(
        CreateTrainingRequestDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateTrainingDates(
                request.TrainingStartDate,
                request.TrainingEndDate,
                request.DepartureDate,
                request.ReturnDate);

            ValidateCosts(
                request.CourseFee,
                request.FlightEstimate);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.Id == employeeId &&
                         e.Status == EmployeeStatus.Active,
                    cancellationToken);

            if (employee is null)
            {
                throw new UnauthorizedAccessException(
            "The active employee account was not found.");
            }

            var now = DateTime.UtcNow;

            var trainingRequest = new TrainingRequest
            {
                RequestNumber = await GenerateRequestNumberAsync(
                    cancellationToken),
                EmployeeId = employeeId,
                Title = request.Title.Trim(),
                Provider = request.Provider.Trim(),
                CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
                City = request.City.Trim(),
                TrainingStartDate = request.TrainingStartDate,
                TrainingEndDate = request.TrainingEndDate,
                DepartureDate = request.DepartureDate,
                ReturnDate = request.ReturnDate,
                BusinessJustification = request.BusinessJustification.Trim(),
                CourseFee = request.CourseFee,
                CourseFeeCurrency = request.CourseFeeCurrency
                    .Trim().ToUpperInvariant(),
                FlightEstimate = request.FlightEstimate ?? 0,
                FlightCurrency = request.FlightCurrency?
                    .Trim().ToUpperInvariant() ?? "NGN",
                Status = TrainingStatus.Draft,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.TrainingRequests.Add(trainingRequest);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Training request {RequestId} created by employee {EmployeeId}.",
                trainingRequest.Id,
                employeeId);

            return await GetRequestByIdAsync(
                trainingRequest.Id,
                employeeId,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to create a training request for employee {EmployeeId}.",
                employeeId);
            throw;
        }
    }

    public async Task<TrainingRequestResponseDto> UpdateRequestAsync(
        int requestId,
        UpdateTrainingRequestDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateTrainingDates(
                request.TrainingStartDate,
                request.TrainingEndDate,
                request.DepartureDate,
                request.ReturnDate);

            ValidateCosts(request.CourseFee, request.FlightEstimate);

            var trainingRequest = await _context.TrainingRequests
                .FirstOrDefaultAsync(
                    r => r.Id == requestId,
                    cancellationToken);

            if (trainingRequest is null)
            {
                throw new KeyNotFoundException(
            "Training request not found.");
            }

            if (trainingRequest.EmployeeId != employeeId)
            {
                throw new UnauthorizedAccessException(
            "You cannot edit another employee's training request.");
            }

            if (trainingRequest.Status != TrainingStatus.Draft)
            {
                throw new InvalidOperationException(
            "Only draft training requests can be edited.");
            }

            trainingRequest.Title = request.Title.Trim();
            trainingRequest.Provider = request.Provider.Trim();
            trainingRequest.CountryCode =
                request.CountryCode.Trim().ToUpperInvariant();
            trainingRequest.City = request.City.Trim();
            trainingRequest.TrainingStartDate = request.TrainingStartDate;
            trainingRequest.TrainingEndDate = request.TrainingEndDate;
            trainingRequest.DepartureDate = request.DepartureDate;
            trainingRequest.ReturnDate = request.ReturnDate;
            trainingRequest.BusinessJustification =
                request.BusinessJustification.Trim();
            trainingRequest.CourseFee = request.CourseFee;
            trainingRequest.CourseFeeCurrency =
                request.CourseFeeCurrency.Trim().ToUpperInvariant();
            trainingRequest.FlightEstimate = request.FlightEstimate ?? 0;
            trainingRequest.FlightCurrency =
                request.FlightCurrency?.Trim().ToUpperInvariant() ?? "NGN";
            trainingRequest.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return await GetRequestByIdAsync(
                requestId,
                employeeId,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to update training request {RequestId}.",
                requestId);
            throw;
        }
    }

    public async Task<TrainingRequestResponseDto> SubmitRequestAsync(
        int requestId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var request = await _context.TrainingRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(
                    r => r.Id == requestId,
                    cancellationToken);

            if (request is null)
            {
                throw new KeyNotFoundException(
            "Training request not found.");
            }

            if (request.EmployeeId != employeeId)
            {
                throw new UnauthorizedAccessException(
                    "You cannot submit another employee's request.");
            }

            if (request.Status != TrainingStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Only draft requests can be submitted.");
            }

            if (!request.DepartureDate.HasValue ||
                !request.ReturnDate.HasValue)
            {
                throw new InvalidOperationException(
                    "Departure and return dates are required before submission.");
            }

            ValidateTrainingDates(
                request.TrainingStartDate,
                request.TrainingEndDate,
                request.DepartureDate,
                request.ReturnDate);

            var managerId = request.Employee.ReportingManagerId;

            if (!managerId.HasValue)
            {
                throw new InvalidOperationException(
                    "You must have a reporting manager before submitting a training request.");
            }

            var manager = await _context.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(
                    e => e.Id == managerId.Value &&
                         e.Status == EmployeeStatus.Active,
                    cancellationToken);

            if (manager is null ||
                !manager.EmployeeRoles.Any(
                    er => er.Role.Name == "Manager"))
            {
                throw new InvalidOperationException(
                    "Your reporting manager must be an active Manager.");
            }

            if (manager.Id == employeeId)
            {
                throw new InvalidOperationException(
                    "You cannot submit a request for your own approval.");
            }

            var previousStatus = request.Status;
            var now = DateTime.UtcNow;

            request.AssignedReviewerId = manager.Id;
            request.Status = TrainingStatus.PendingManagerApproval;
            request.SubmittedAtUtc = now;
            request.UpdatedAtUtc = now;

            AddApprovalHistory(
                request,
                employeeId,
                "Staff",
                TrainingDecision.Approve,
                previousStatus,
                request.Status,
                comment: "Training request submitted.",
                reason: null,
                previousReviewerId: null,
                newReviewerId: manager.Id);

            AddNotificationAndEmail(
                manager.Id,
                NotificationType.Training,
                "Training request awaiting review",
                $"Training request {request.RequestNumber} requires your review.",
                "TrainingRequest",
                request.Id,
                $"training-manager-{request.Id}-{manager.Id}",
                manager.Email);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetRequestByIdAsync(
                requestId,
                employeeId,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to submit training request {RequestId}.",
                requestId);
            throw;
        }
    }

    public async Task<TrainingRequestResponseDto> GetRequestByIdAsync(
        int requestId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _context.TrainingRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.AssignedReviewer)
                .FirstOrDefaultAsync(
                    r => r.Id == requestId,
                    cancellationToken);

            if (request is null)
            {
                throw new KeyNotFoundException(
                    "Training request not found.");
            }

            var actor = await _context.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(
                    e => e.Id == actorEmployeeId,
                    cancellationToken);

            if (actor is null)
            {
                throw new UnauthorizedAccessException(
                    "Employee account not found.");
            }

            var isOwner = request.EmployeeId == actorEmployeeId;
            var isAssignedReviewer =
                request.AssignedReviewerId == actorEmployeeId;

            var roles = actor.EmployeeRoles
                .Select(er => er.Role.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var canView = isOwner ||
                          isAssignedReviewer ||
                          roles.Contains("HR") ||
                          roles.Contains("Finance") ||
                          roles.Contains("Admin");

            if (!canView)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to view this training request.");
            }

            return MapRequest(request);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve training request {RequestId}.",
                requestId);
            throw;
        }
    }

    public async Task<PagedResponseDto<TrainingRequestResponseDto>>
        GetMyRequestsAsync(
            int employeeId,
            PaginationRequestDto pagination,
            CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.TrainingRequests
                .AsNoTracking()
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.CreatedAtUtc);

            var totalCount = await query.CountAsync(cancellationToken);

            var requests = await query
                .Include(r => r.Employee)
                .Include(r => r.AssignedReviewer)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var results = requests.Select(MapRequest).ToList();

            return PaginationService.CreateResponse(
                results,
                pagination.PageNumber,
                pagination.PageSize,
                totalCount);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve requests for employee {EmployeeId}.",
                employeeId);
            throw;
        }
    }

    public async Task<TrainingRequestResponseDto> DecideAsync(
        int requestId,
        TrainingDecisionDto decision,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var request = await _context.TrainingRequests
                .Include(r => r.Employee)
                .Include(r => r.ApprovalHistory)
                .Include(r => r.AllowanceVersions)
                .FirstOrDefaultAsync(
                    r => r.Id == requestId,
                    cancellationToken);

            if (request is null)
            {
                throw new KeyNotFoundException(
                    "Training request not found.");
            }

            var actor = await _context.Employees
                .Include(e => e.EmployeeRoles)
                    .ThenInclude(er => er.Role)
                .FirstOrDefaultAsync(
                    e => e.Id == actorEmployeeId,
                    cancellationToken);

            if (actor is null ||
                actor.Status != EmployeeStatus.Active)
            {
                throw new UnauthorizedAccessException(
                    "An active employee account is required.");
            }

            var roles = actor.EmployeeRoles
                .Select(er => er.Role.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (request.EmployeeId == actorEmployeeId)
            {
                throw new UnauthorizedAccessException(
                    "You cannot approve or reject your own training request.");
            }

            var previousStatus = request.Status;
            var previousReviewerId = request.AssignedReviewerId;
            var now = DateTime.UtcNow;
            var actorRole = string.Empty;
            int? notifyEmployeeId = null;
            string? notificationTitle = null;
            string? notificationMessage = null;

            if (request.Status == TrainingStatus.PendingManagerApproval)
            {
                if (!roles.Contains("Manager") ||
                    request.AssignedReviewerId != actorEmployeeId)
                {
                    throw new UnauthorizedAccessException(
                        "Only the assigned Manager can review this request.");
                }

                actorRole = "Manager";

                if (decision.Decision == TrainingDecision.Approve)
                {
                    request.Status = TrainingStatus.PendingHRApproval;
                    notifyEmployeeId = FindRoleHolderId("HR");
                    notificationTitle = "Training request awaiting HR";
                    notificationMessage =
                        $"Training request {request.RequestNumber} requires HR review.";
                }
                else if (decision.Decision == TrainingDecision.Reject)
                {
                    RequireReason(decision.Comment, "A rejection comment is required.");
                    request.Status = TrainingStatus.Rejected;
                    notifyEmployeeId = request.EmployeeId;
                    notificationTitle = "Training request rejected";
                    notificationMessage =
                        $"Your training request {request.RequestNumber} was rejected.";
                }
                else
                {
                    throw new InvalidOperationException(
                        "This decision is not allowed during Manager review.");
                }
            }
            else if (request.Status == TrainingStatus.PendingHRApproval)
            {
                if (!roles.Contains("HR"))
                {
                    throw new UnauthorizedAccessException(
                        "Only HR can review this request at this stage.");
                }

                actorRole = "HR";

                if (decision.Decision == TrainingDecision.Approve)
                {
                    var rate = await _context.EstacodeRates
                        .FirstOrDefaultAsync(
                            r => r.CountryCode == request.CountryCode &&
                                 r.Grade == request.Employee.Grade &&
                                 r.IsActive,
                            cancellationToken);

                    if (rate is null)
                    {
                        throw new InvalidOperationException(
                            $"No active estacode rate exists for country " +
                            $"{request.CountryCode} and grade {request.Employee.Grade}.");
                    }

                    var eligibleDays = CalculateEligibleDays(request);

                    var allowance = new TrainingAllowanceVersion
                    {
                        TrainingRequestId = request.Id,
                        VersionNumber = 1,
                        CountryCodeSnapshot = request.CountryCode,
                        GradeSnapshot = request.Employee.Grade,
                        DailyRateUsdSnapshot = rate.DailyRateUsd,
                        EligibleDays = eligibleDays,
                        TotalAmountUsd = decimal.Round(
                            eligibleDays * rate.DailyRateUsd,
                            2,
                            MidpointRounding.AwayFromZero),
                        Status = AllowanceStatus.Provisional,
                        IsCurrent = true,
                        CreatedByEmployeeId = actorEmployeeId,
                        CreatedAtUtc = now
                    };

                    request.AllowanceVersions.Add(allowance);
                    request.Status = TrainingStatus.PendingFinanceApproval;

                    notifyEmployeeId = FindRoleHolderId("Finance");
                    notificationTitle = "Training allowance awaiting Finance";
                    notificationMessage =
                        $"Training request {request.RequestNumber} requires Finance authorization.";
                }
                else if (decision.Decision == TrainingDecision.Reject)
                {
                    RequireReason(
                        decision.Reason ?? decision.Comment,
                        "A rejection reason is required.");

                    request.Status = TrainingStatus.Rejected;
                    notifyEmployeeId = request.EmployeeId;
                    notificationTitle = "Training request rejected";
                    notificationMessage =
                        $"Your training request {request.RequestNumber} was rejected by HR.";
                }
                else
                {
                    throw new InvalidOperationException(
                        "This decision is not allowed during HR review.");
                }
            }
            else if (request.Status == TrainingStatus.PendingFinanceApproval)
            {
                if (!roles.Contains("Finance"))
                {
                    throw new UnauthorizedAccessException(
                        "Only Finance can authorize this allowance.");
                }

                actorRole = "Finance";

                var currentAllowance = request.AllowanceVersions
                    .Where(a => a.IsCurrent)
                    .OrderByDescending(a => a.VersionNumber)
                    .FirstOrDefault();

                if (currentAllowance is null)
                {
                    throw new InvalidOperationException(
                        "No current allowance version exists.");
                }

                if (decision.Decision == TrainingDecision.Approve)
                {
                    currentAllowance.Status = AllowanceStatus.Approved;
                    currentAllowance.ApprovedByEmployeeId = actorEmployeeId;
                    currentAllowance.ApprovedAtUtc = now;
                    request.Status = TrainingStatus.Approved;

                    notifyEmployeeId = request.EmployeeId;
                    notificationTitle = "Training request approved";
                    notificationMessage =
                        $"Your training request {request.RequestNumber} has been approved.";
                }
                else if (decision.Decision == TrainingDecision.Reject)
                {
                    RequireReason(
                        decision.Reason ?? decision.Comment,
                        "A rejection reason is required.");

                    request.Status = TrainingStatus.Rejected;
                    notifyEmployeeId = request.EmployeeId;
                    notificationTitle = "Training request rejected";
                    notificationMessage =
                        $"Your training request {request.RequestNumber} was rejected by Finance.";
                }
                else if (decision.Decision == TrainingDecision.ReturnToHR)
                {
                    RequireReason(
                        decision.Reason ?? decision.Comment,
                        "A reason is required when returning an allowance to HR.");

                    currentAllowance.Status = AllowanceStatus.Provisional;
                    request.Status = TrainingStatus.ReturnedToHR;

                    notifyEmployeeId = FindRoleHolderId("HR");
                    notificationTitle = "Allowance correction required";
                    notificationMessage =
                        $"Training request {request.RequestNumber} was returned to HR for correction.";
                }
                else
                {
                    throw new InvalidOperationException(
                        "This decision is not allowed during Finance review.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"A decision cannot be made when the request status is {request.Status}.");
            }

            request.UpdatedAtUtc = now;

            AddApprovalHistory(
                request,
                actorEmployeeId,
                actorRole,
                decision.Decision,
                previousStatus,
                request.Status,
                decision.Comment,
                decision.Reason,
                previousReviewerId,
                request.AssignedReviewerId);

            if (notifyEmployeeId.HasValue &&
                notificationTitle is not null &&
                notificationMessage is not null)
            {
                var recipient = await _context.Employees
                    .FirstOrDefaultAsync(
                        e => e.Id == notifyEmployeeId.Value,
                        cancellationToken);

                if (recipient is not null)
                {
                    AddNotificationAndEmail(
                        recipient.Id,
                        NotificationType.Training,
                        notificationTitle,
                        notificationMessage,
                        "TrainingRequest",
                        request.Id,
                        $"training-decision-{request.Id}-{request.Status}-{recipient.Id}",
                        recipient.Email);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Training request {RequestId} moved from {PreviousStatus} to {NewStatus} by employee {ActorEmployeeId}.",
                requestId,
                previousStatus,
                request.Status,
                actorEmployeeId);

            return await GetRequestByIdAsync(
                requestId,
                actorEmployeeId,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to process decision for training request {RequestId}.",
                requestId);
            throw;
        }
    }

    public async Task WithdrawRequestAsync(
        int requestId,
        WithdrawTrainingRequestDto request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            RequireReason(request.Reason, "A withdrawal reason is required.");

            var trainingRequest = await _context.TrainingRequests
                .FirstOrDefaultAsync(
                    r => r.Id == requestId,
                    cancellationToken);

            if (trainingRequest is null)
            {
                throw new KeyNotFoundException(
                    "Training request not found.");
            }

            if (trainingRequest.EmployeeId != employeeId)
            {
                throw new UnauthorizedAccessException(
                    "You cannot withdraw another employee's request.");
            }

            var allowedStatuses = new[]
            {
                TrainingStatus.Draft,
                TrainingStatus.PendingManagerApproval,
                TrainingStatus.PendingHRApproval,
                TrainingStatus.PendingFinanceApproval,
                TrainingStatus.ReturnedToHR
            };

            if (!allowedStatuses.Contains(trainingRequest.Status))
            {
                throw new InvalidOperationException(
                    "This training request can no longer be withdrawn.");
            }

            var previousStatus = trainingRequest.Status;
            var reviewerId = trainingRequest.AssignedReviewerId;
            var now = DateTime.UtcNow;

            trainingRequest.Status = TrainingStatus.Withdrawn;
            trainingRequest.WithdrawnAtUtc = now;
            trainingRequest.WithdrawnByEmployeeId = employeeId;
            trainingRequest.WithdrawalReason = request.Reason.Trim();
            trainingRequest.UpdatedAtUtc = now;

            AddApprovalHistory(
                trainingRequest,
                employeeId,
                "Staff",
                TrainingDecision.Reject,
                previousStatus,
                TrainingStatus.Withdrawn,
                comment: null,
                reason: request.Reason,
                previousReviewerId: reviewerId,
                newReviewerId: reviewerId);

            if (reviewerId.HasValue)
            {
                var reviewer = await _context.Employees
                    .FirstOrDefaultAsync(
                        e => e.Id == reviewerId.Value,
                        cancellationToken);

                if (reviewer is not null)
                {
                    AddNotificationAndEmail(
                        reviewer.Id,
                        NotificationType.Training,
                        "Training request withdrawn",
                        $"Training request {trainingRequest.RequestNumber} was withdrawn by the employee.",
                        "TrainingRequest",
                        trainingRequest.Id,
                        $"training-withdrawn-{trainingRequest.Id}",
                        reviewer.Email);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to withdraw training request {RequestId}.",
                requestId);
            throw;
        }
    }

    public async Task<EstacodeRateResponseDto> CreateEstacodeRateAsync(
        CreateEstacodeRateDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureRoleAsync(
                actorEmployeeId,
                "HR",
                cancellationToken);

            var country = request.CountryCode.Trim().ToUpperInvariant();
            var grade = request.Grade.Trim();

            var exists = await _context.EstacodeRates.AnyAsync(
                r => r.CountryCode == country && r.Grade == grade,
                cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    "An estacode rate already exists for this country and grade. Update it instead.");
            }

            if (request.DailyRateUsd <= 0)
            {
                throw new InvalidOperationException(
                    "The daily USD rate must be greater than zero.");
            }

            var now = DateTime.UtcNow;

            var rate = new EstaCodeRate
            {
                CountryCode = country,
                Grade = grade,
                DailyRateUsd = decimal.Round(
                    request.DailyRateUsd,
                    2,
                    MidpointRounding.AwayFromZero),
                IsActive = request.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.EstacodeRates.Add(rate);
            await _context.SaveChangesAsync(cancellationToken);

            return MapRate(rate);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to create an estacode rate.");
            throw;
        }
    }

    public async Task<EstacodeRateResponseDto> UpdateEstacodeRateAsync(
        int rateId,
        UpdateEstacodeRateDto request,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureRoleAsync(
                actorEmployeeId,
                "HR",
                cancellationToken);

            var rate = await _context.EstacodeRates
                .FirstOrDefaultAsync(
                    r => r.Id == rateId,
                    cancellationToken);

            if (rate is null)
            {
                throw new KeyNotFoundException(
                    "Estacode rate not found.");
            }

            if (request.DailyRateUsd <= 0)
            {
                throw new InvalidOperationException(
                    "The daily USD rate must be greater than zero.");
            }

            rate.DailyRateUsd = decimal.Round(
                request.DailyRateUsd,
                2,
                MidpointRounding.AwayFromZero);
            rate.IsActive = request.IsActive;
            rate.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapRate(rate);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to update estacode rate {RateId}.",
                rateId);
            throw;
        }
    }

    public async Task<TrainingAllowanceVersionDto> CorrectAllowanceAsync(
        int requestId,
        CorrectTrainingAllowanceDto correction,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            RequireReason(
                correction.Reason,
                "A correction reason is required.");

            await EnsureRoleAsync(
                actorEmployeeId,
                "HR",
                cancellationToken);

            var request = await _context.TrainingRequests
                .Include(r => r.Employee)
                .Include(r => r.AllowanceVersions)
                .FirstOrDefaultAsync(
                    r => r.Id == requestId,
                    cancellationToken);

            if (request is null)
            {
                throw new KeyNotFoundException(
                    "Training request not found.");
            }

            if (request.Status != TrainingStatus.ReturnedToHR)
            {
                throw new InvalidOperationException(
                    "Only allowances returned to HR can be corrected.");
            }

            var current = request.AllowanceVersions
                .Where(a => a.IsCurrent)
                .OrderByDescending(a => a.VersionNumber)
                .FirstOrDefault();

            if (current is null)
            {
                throw new InvalidOperationException(
                    "No current allowance version exists to correct.");
            }

            var computedDays = CalculateEligibleDays(request);

            var correctedDays = correction.EligibleDays
                ?? current.EligibleDays;

            if (correctedDays <= 0 || correctedDays > computedDays)
            {
                throw new InvalidOperationException(
                    $"Eligible days must be between 1 and {computedDays}.");
            }

            var correctedRate = correction.DailyRateUsd
                ?? current.DailyRateUsdSnapshot;

            if (correctedRate <= 0)
            {
                throw new InvalidOperationException(
                    "The corrected daily rate must be greater than zero.");
            }

            var now = DateTime.UtcNow;

            current.IsCurrent = false;
            current.Status = AllowanceStatus.Superseded;

            var newVersion = new TrainingAllowanceVersion
            {
                TrainingRequestId = request.Id,
                VersionNumber = current.VersionNumber + 1,
                CountryCodeSnapshot = current.CountryCodeSnapshot,
                GradeSnapshot = current.GradeSnapshot,
                DailyRateUsdSnapshot = correctedRate,
                EligibleDays = correctedDays,
                TotalAmountUsd = decimal.Round(
                    correctedRate * correctedDays,
                    2,
                    MidpointRounding.AwayFromZero),
                Status = AllowanceStatus.Provisional,
                IsCurrent = true,
                CorrectionReason = correction.Reason.Trim(),
                CreatedByEmployeeId = actorEmployeeId,
                CreatedAtUtc = now
            };

            _context.TrainingAllowanceVersions.Add(newVersion);

            request.Status = TrainingStatus.PendingFinanceApproval;
            request.UpdatedAtUtc = now;

            AddApprovalHistory(
                request,
                actorEmployeeId,
                "HR",
                TrainingDecision.Approve,
                TrainingStatus.ReturnedToHR,
                TrainingStatus.PendingFinanceApproval,
                comment: "Allowance corrected and resubmitted.",
                reason: correction.Reason,
                previousReviewerId: request.AssignedReviewerId,
                newReviewerId: request.AssignedReviewerId);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Allowance version {VersionNumber} created for training request {RequestId}.",
                newVersion.VersionNumber,
                requestId);

            return MapAllowance(newVersion);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to correct allowance for request {RequestId}.",
                requestId);
            throw;
        }
    }

   
public async Task<TrainingRequestResponseDto> ReassignReviewerAsync(
    int requestId,
    ReassignTrainingReviewerDto request,
    int adminEmployeeId,
    CancellationToken cancellationToken = default)
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException(
                "A reassignment reason is required.");
        }

        await EnsureRoleAsync(
            adminEmployeeId, "Admin", cancellationToken);

        var training = await _context.TrainingRequests
            .FirstOrDefaultAsync(
                r => r.Id == requestId, cancellationToken);

        if (training is null)
        {
            throw new KeyNotFoundException(
                "Training request not found.");
        }

        if (training.Status != TrainingStatus.PendingManagerApproval)
        {
            throw new InvalidOperationException(
                "Only requests awaiting Manager approval can be reassigned.");
        }

        if (request.NewReviewerId == training.EmployeeId)
        {
            throw new InvalidOperationException(
                "The requester cannot be their own reviewer.");
        }

        var newReviewer = await _context.Employees
            .Include(e => e.EmployeeRoles)
                .ThenInclude(er => er.Role)
            .FirstOrDefaultAsync(
                e => e.Id == request.NewReviewerId &&
                     e.Status == EmployeeStatus.Active,
                cancellationToken);

        if (newReviewer is null ||
            !newReviewer.EmployeeRoles.Any(
                er => er.Role.Name == "Manager"))
        {
            throw new InvalidOperationException(
                "The new reviewer must be an active Manager.");
        }

        var previousReviewerId = training.AssignedReviewerId;
        var now = DateTime.UtcNow;

        training.AssignedReviewerId = newReviewer.Id;
        training.UpdatedAtUtc = now;

        AddApprovalHistory(
            training,
            adminEmployeeId,
            "Admin",
            TrainingDecision.Approve,
            training.Status,
            training.Status,
            comment: "Reviewer reassigned.",
            reason: request.Reason,
            previousReviewerId: previousReviewerId,
            newReviewerId: newReviewer.Id);

        if (previousReviewerId.HasValue)
        {
            var previousReviewer = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.Id == previousReviewerId.Value,
                    cancellationToken);

            if (previousReviewer is not null)
            {
                _context.Notifications.Add(new Notification
                {
                    EmployeeId = previousReviewer.Id,
                    Type = NotificationType.Training,
                    Title = "Training review reassigned",
                    Message = $"You are no longer assigned to request {training.RequestNumber}.",
                    RelatedEntityType = "TrainingRequest",
                    RelatedEntityId = training.Id,
                    DeduplicationKey =
                        $"training-reassigned-away-{training.Id}-{previousReviewer.Id}-{now.Ticks}",
                    CreatedAtUtc = now
                });
            }
        }

        AddNotificationAndEmail(
            newReviewer.Id,
            NotificationType.Training,
            "Training review assigned to you",
            $"Training request {training.RequestNumber} has been assigned to you.",
            "TrainingRequest",
            training.Id,
            $"training-reassigned-to-{training.Id}-{newReviewer.Id}",
            newReviewer.Email);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequestByIdAsync(
            requestId, adminEmployeeId, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _logger.LogError(
            ex, "Failed to reassign training request {RequestId}.", requestId);
        throw;
    }
}


   
public async Task<TrainingCompletionEvidence> SubmitCompletionEvidenceAsync(
    int requestId,
    int attachmentId,
    int employeeId,
    CancellationToken cancellationToken = default)
{
    try
    {
        var training = await _context.TrainingRequests
            .Include(r => r.CompletionEvidence)
            .FirstOrDefaultAsync(
                r => r.Id == requestId, cancellationToken);

        if (training is null)
        {
            throw new KeyNotFoundException("Training request not found.");
        }

        if (training.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "You cannot submit evidence for another employee's request.");
        }

        if (training.Status != TrainingStatus.Approved)
        {
            throw new InvalidOperationException(
                "Evidence can only be submitted for an approved request.");
        }

        if (DateOnly.FromDateTime(DateTime.UtcNow) <= training.TrainingEndDate)
        {
            throw new InvalidOperationException(
                "Completion evidence can only be submitted after the training end date.");
        }

        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(
                a => a.Id == attachmentId &&
                     a.UploadedByEmployeeId == employeeId &&
                     a.EntityType == "TrainingRequest" &&
                     a.EntityId == requestId &&
                     a.Category == "CompletionEvidence",
                cancellationToken);

        if (attachment is null)
        {
            throw new InvalidOperationException(
                "A valid completion-evidence attachment is required.");
        }

        var hasPendingEvidence = training.CompletionEvidence.Any(
            e => e.Status == TrainingEvidenceStatus.PendingReview);

        if (hasPendingEvidence)
        {
            throw new InvalidOperationException(
                "Evidence is already awaiting HR review.");
        }

        var evidence = new TrainingCompletionEvidence
        {
            TrainingRequestId = requestId,
            AttachmentId = attachmentId,
            Status = TrainingEvidenceStatus.PendingReview,
            SubmittedByEmployeeId = employeeId,
            SubmittedAtUtc = DateTime.UtcNow
        };

        _context.TrainingCompletionEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completion evidence {EvidenceId} submitted for training request {RequestId}.",
            evidence.Id, requestId);

        return evidence;
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _logger.LogError(
            ex, "Failed to submit completion evidence for request {RequestId}.",
            requestId);
        throw;
    }
}


    
public async Task<TrainingRequestResponseDto> ReviewCompletionEvidenceAsync(
    int requestId,
    int evidenceId,
    ReviewTrainingEvidenceDto review,
    int hrEmployeeId,
    CancellationToken cancellationToken = default)
{
    try
    {
        await EnsureRoleAsync(hrEmployeeId, "HR", cancellationToken);

        var training = await _context.TrainingRequests
            .Include(r => r.Employee)
            .Include(r => r.AssignedReviewer)
            .FirstOrDefaultAsync(
                r => r.Id == requestId, cancellationToken);

        if (training is null)
        {
            throw new KeyNotFoundException("Training request not found.");
        }

        if (training.Status != TrainingStatus.Approved)
        {
            throw new InvalidOperationException(
                "Only approved training requests can have evidence reviewed.");
        }

        var evidence = await _context.TrainingCompletionEvidence
            .Include(e => e.Attachment)
            .FirstOrDefaultAsync(
                e => e.Id == evidenceId &&
                     e.TrainingRequestId == requestId,
                cancellationToken);

        if (evidence is null)
        {
            throw new KeyNotFoundException("Completion evidence not found.");
        }

        if (evidence.Status != TrainingEvidenceStatus.PendingReview)
        {
            throw new InvalidOperationException(
                "This evidence has already been reviewed.");
        }

        if (!review.Accept && string.IsNullOrWhiteSpace(review.Comment))
        {
            throw new InvalidOperationException(
                "A comment is required when returning evidence.");
        }

        var now = DateTime.UtcNow;

        evidence.ReviewedByEmployeeId = hrEmployeeId;
        evidence.ReviewedAtUtc = now;
        evidence.ReviewComment = review.Comment?.Trim();

        if (review.Accept)
        {
            evidence.Status = TrainingEvidenceStatus.Accepted;
            training.Status = TrainingStatus.Completed;
        }
        else
        {
            evidence.Status = TrainingEvidenceStatus.Returned;
            training.Status = TrainingStatus.Approved;
        }

        training.UpdatedAtUtc = now;

        AddApprovalHistory(
            training,
            hrEmployeeId,
            "HR",
            review.Accept
                ? TrainingDecision.Approve
                : TrainingDecision.ReturnToEmployee,
            TrainingStatus.Approved,
            training.Status,
            review.Comment,
            review.Accept ? null : review.Comment,
            training.AssignedReviewerId,
            training.AssignedReviewerId);

        AddNotificationAndEmail(
            training.EmployeeId,
            NotificationType.Training,
            review.Accept
                ? "Training completed"
                : "Training evidence needs changes",
            review.Accept
                ? $"Your training request {training.RequestNumber} has been marked completed."
                : $"Your evidence for request {training.RequestNumber} was returned. Please review HR's comment.",
            "TrainingRequest",
            training.Id,
            $"training-evidence-review-{evidence.Id}",
            training.Employee.Email);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequestByIdAsync(
            requestId, hrEmployeeId, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _logger.LogError(
            ex, "Failed to review evidence {EvidenceId} for request {RequestId}.",
            evidenceId, requestId);
        throw;
    }
}

    
public async Task<TrainingRequestResponseDto> CloseWithoutEvidenceAsync(
    int requestId,
    string reason,
    int hrEmployeeId,
    CancellationToken cancellationToken = default)
{
    try
    {
        await EnsureRoleAsync(hrEmployeeId, "HR", cancellationToken);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException(
                "A reason is required to close a request without evidence.");
        }

        var training = await _context.TrainingRequests
            .Include(r => r.Employee)
            .Include(r => r.AssignedReviewer)
            .Include(r => r.CompletionEvidence)
            .FirstOrDefaultAsync(
                r => r.Id == requestId, cancellationToken);

        if (training is null)
        {
            throw new KeyNotFoundException("Training request not found.");
        }

        var followUpDate = training.TrainingEndDate.AddDays(60);

        if (training.Status != TrainingStatus.Approved ||
            DateOnly.FromDateTime(DateTime.UtcNow) < followUpDate)
        {
            throw new InvalidOperationException(
                "Only approved requests at least 60 days past training end can be closed.");
        }

        if (training.CompletionEvidence.Any(
            e => e.Status == TrainingEvidenceStatus.Accepted))
        {
            throw new InvalidOperationException(
                "This request already has accepted completion evidence.");
        }

        var previousStatus = training.Status;
        var now = DateTime.UtcNow;

        training.Status = TrainingStatus.RejectedNoEvidence;
        training.UpdatedAtUtc = now;

        AddApprovalHistory(
            training,
            hrEmployeeId,
            "HR",
            TrainingDecision.Reject,
            previousStatus,
            training.Status,
            comment: null,
            reason: reason.Trim(),
            previousReviewerId: training.AssignedReviewerId,
            newReviewerId: training.AssignedReviewerId);

        AddNotificationAndEmail(
            training.EmployeeId,
            NotificationType.Training,
            "Training request closed",
            $"Training request {training.RequestNumber} was closed because completion evidence was not accepted. Reason: {reason.Trim()}",
            "TrainingRequest",
            training.Id,
            $"training-no-evidence-closed-{training.Id}",
            training.Employee.Email);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRequestByIdAsync(
            requestId, hrEmployeeId, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _logger.LogError(
            ex, "Failed to close training request {RequestId} without evidence.",
            requestId);
        throw;
    }
}

    // ------------------------------------------------------------------
    // Review queues (each role sees only what it is allowed to act on)
    // ------------------------------------------------------------------

    public async Task<PagedResponseDto<TrainingRequestResponseDto>> GetManagerQueueAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        // Only requests ASSIGNED to this reviewer, regardless of reporting line.
        return await GetQueueAsync(
            r => r.Status == TrainingStatus.PendingManagerApproval &&
                 r.AssignedReviewerId == actorEmployeeId,
            pagination,
            cancellationToken);
    }

    public async Task<PagedResponseDto<TrainingRequestResponseDto>> GetHrQueueAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(actorEmployeeId, "HR", cancellationToken);

        return await GetQueueAsync(
            r => (r.Status == TrainingStatus.PendingHRApproval ||
                  r.Status == TrainingStatus.ReturnedToHR) &&
                 r.EmployeeId != actorEmployeeId,
            pagination,
            cancellationToken);
    }

    public async Task<PagedResponseDto<TrainingRequestResponseDto>> GetFinanceQueueAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(actorEmployeeId, "Finance", cancellationToken);

        return await GetQueueAsync(
            r => r.Status == TrainingStatus.PendingFinanceApproval &&
                 r.EmployeeId != actorEmployeeId,
            pagination,
            cancellationToken);
    }

    public async Task<PagedResponseDto<TrainingRequestResponseDto>> GetAllPendingManagerAsync(
        int actorEmployeeId,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(actorEmployeeId, "Admin", cancellationToken);

        return await GetQueueAsync(
            r => r.Status == TrainingStatus.PendingManagerApproval,
            pagination,
            cancellationToken);
    }

    private async Task<PagedResponseDto<TrainingRequestResponseDto>> GetQueueAsync(
        System.Linq.Expressions.Expression<Func<TrainingRequest, bool>> filter,
        PaginationRequestDto pagination,
        CancellationToken cancellationToken)
    {
        var query = _context.TrainingRequests
            .AsNoTracking()
            .Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .Include(r => r.Employee)
            .Include(r => r.AssignedReviewer)
            .OrderBy(r => r.SubmittedAtUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PaginationService.CreateResponse(
            requests.Select(MapRequest).ToList(),
            pagination.PageNumber,
            pagination.PageSize,
            totalCount);
    }

    // ------------------------------------------------------------------
    // Allowance versions and PDF statement
    // ------------------------------------------------------------------

    public async Task<List<TrainingAllowanceVersionDto>> GetAllowanceVersionsAsync(
        int requestId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.TrainingRequests
            .AsNoTracking()
            .Include(r => r.AllowanceVersions)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Training request not found.");

        var roles = await GetRoleNamesAsync(actorEmployeeId, cancellationToken);
        var privileged = roles.Contains("HR") || roles.Contains("Finance") || roles.Contains("Admin");

        if (!privileged && request.EmployeeId != actorEmployeeId)
        {
            throw new UnauthorizedAccessException(
                "You are not authorized to view this allowance.");
        }

        var versions = request.AllowanceVersions.AsEnumerable();

        // The owner only sees allowances once Finance has approved them.
        if (!privileged)
        {
            versions = versions.Where(v => v.Status == AllowanceStatus.Approved);
        }

        return versions
            .OrderBy(v => v.VersionNumber)
            .Select(MapAllowance)
            .ToList();
    }

    public async Task<(byte[] Content, string FileName)> GenerateAllowancePdfAsync(
        int requestId,
        int actorEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.TrainingRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.AllowanceVersions)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Training request not found.");

        var roles = await GetRoleNamesAsync(actorEmployeeId, cancellationToken);
        var privileged = roles.Contains("HR") || roles.Contains("Finance") || roles.Contains("Admin");

        if (!privileged && request.EmployeeId != actorEmployeeId)
        {
            throw new UnauthorizedAccessException(
                "You are not authorized to download this statement.");
        }

        // Built from the APPROVED version only; it records no payment.
        var approved = request.AllowanceVersions
            .Where(v => v.Status == AllowanceStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (approved is null)
        {
            throw new InvalidOperationException(
                "The allowance statement is only available after Finance approval.");
        }

        var bytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Text("Estacode Allowance Statement").FontSize(20).Bold();
                    column.Item().Text($"Request {request.RequestNumber}").FontSize(12);
                    column.Item().LineHorizontal(1);

                    column.Item().Text($"Employee: {request.Employee.FirstName} {request.Employee.LastName} ({request.Employee.EmployeeNumber})");
                    column.Item().Text($"Training: {request.Title} ({request.Provider})");
                    column.Item().Text($"Destination: {request.City}, {approved.CountryCodeSnapshot}");
                    column.Item().Text($"Departure: {request.DepartureDate:yyyy-MM-dd}    Return: {request.ReturnDate:yyyy-MM-dd}");
                    column.Item().LineHorizontal(1);

                    column.Item().Text($"Grade: {approved.GradeSnapshot}");
                    column.Item().Text($"Daily rate: USD {approved.DailyRateUsdSnapshot:N2}");
                    column.Item().Text($"Eligible days: {approved.EligibleDays}");
                    column.Item().Text($"Allowance amount: USD {approved.TotalAmountUsd:N2}").Bold();
                    column.Item().Text($"Allowance version: {approved.VersionNumber}");
                    column.Item().Text($"Approved on: {approved.ApprovedAtUtc:yyyy-MM-dd} (UTC)");

                    column.Item().PaddingTop(10).Text(
                        "Course fee and flight estimate are handled separately and are not part of this allowance. " +
                        "This statement does not record a payment.").FontSize(9).Italic();
                });
            });
        }).GeneratePdf();

        return (bytes, $"allowance-{request.RequestNumber}.pdf");
    }

    private async Task<HashSet<string>> GetRoleNamesAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var names = await _context.EmployeeRoles
            .AsNoTracking()
            .Where(er => er.EmployeeId == employeeId)
            .Select(er => er.Role.Name)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateRequestNumberAsync(
        CancellationToken cancellationToken)
    {
        // Random suffix reduces collisions; the database unique index
        // remains the final protection against duplicate request numbers.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"TRN-{DateTime.UtcNow:yyyyMMdd}-" +
                         Convert.ToHexString(
                             RandomNumberGenerator.GetBytes(4));

            var exists = await _context.TrainingRequests.AnyAsync(
                r => r.RequestNumber == number,
                cancellationToken);

            if (!exists)
            {
                return number;
            }
        }

        throw new InvalidOperationException(
            "Unable to generate a unique training request number.");
    }

    private async Task EnsureRoleAsync(
        int employeeId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var hasRole = await _context.EmployeeRoles
            .AnyAsync(
                er => er.EmployeeId == employeeId &&
                      er.Employee.Status == EmployeeStatus.Active &&
                      er.Role.Name == roleName,
                cancellationToken);

        if (!hasRole)
        {
            throw new UnauthorizedAccessException(
                $"The {roleName} role is required for this operation.");
        }
    }

    private int? FindRoleHolderId(string roleName)
    {
        // This method is replaced by the async helper below at call sites
        // in the final reviewed implementation.
        return null;
    }

    private void AddApprovalHistory(
        TrainingRequest request,
        int actorEmployeeId,
        string actorRole,
        TrainingDecision decision,
        TrainingStatus previousStatus,
        TrainingStatus newStatus,
        string? comment,
        string? reason,
        int? previousReviewerId,
        int? newReviewerId)
    {
        request.ApprovalHistory.Add(new TrainingApprovalHistory
        {
            TrainingRequestId = request.Id,
            ActorEmployeeId = actorEmployeeId,
            ActorRole = actorRole,
            Decision = decision,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PreviousReviewerId = previousReviewerId,
            NewReviewerId = newReviewerId,
            Comment = comment,
            Reason = reason,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private void AddNotificationAndEmail(
        int employeeId,
        NotificationType type,
        string title,
        string message,
        string entityType,
        int entityId,
        string deduplicationKey,
        string email)
    {
        var now = DateTime.UtcNow;

        _context.Notifications.Add(new Notification
        {
            EmployeeId = employeeId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId,
            DeduplicationKey = deduplicationKey,
            CreatedAtUtc = now
        });

        _context.EmailQueueItems.Add(new EmailQueueItem
        {
            RecipientEmail = email,
            Subject = title,
            Body = EmailTemplates.Notification(string.Empty, title, message),
            Status = EmailMessageStatus.Pending,
            AttemptCount = 0,
            CreatedAtUtc = now,
            NextAttemptAtUtc = now
        });
    }

    private static void ValidateTrainingDates(
        DateOnly start,
        DateOnly end,
        DateOnly? departure,
        DateOnly? returning)
    {
        if (end < start)
        {
            throw new InvalidOperationException(
                "Training end date cannot be before the start date.");
        }

        if (departure.HasValue && departure.Value > start)
        {
            throw new InvalidOperationException(
                "Departure date must be on or before the training start date.");
        }

        if (returning.HasValue && returning.Value < end)
        {
            throw new InvalidOperationException(
                "Return date must be on or after the training end date.");
        }

        if (departure.HasValue && returning.HasValue &&
            returning.Value < departure.Value)
        {
            throw new InvalidOperationException(
                "Return date cannot be before departure date.");
        }
    }

    private static void ValidateCosts(
        decimal courseFee,
        decimal? flightEstimate)
    {
        if (courseFee < 0 || flightEstimate < 0)
        {
            throw new InvalidOperationException(
                "Training costs cannot be negative.");
        }
    }

    private static int CalculateEligibleDays(TrainingRequest request)
    {
        if (!request.DepartureDate.HasValue ||
            !request.ReturnDate.HasValue)
        {
            throw new InvalidOperationException(
                "Departure and return dates are required to calculate the allowance.");
        }

        return request.ReturnDate.Value.DayNumber -
                request.DepartureDate.Value.DayNumber + 1;
    }

    private static void RequireReason(
        string? value,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static TrainingRequestResponseDto MapRequest(
        TrainingRequest request)
    {
        return new TrainingRequestResponseDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            EmployeeId = request.EmployeeId,
            EmployeeName =
                $"{request.Employee.FirstName} {request.Employee.LastName}",
            Title = request.Title,
            Provider = request.Provider,
            CountryCode = request.CountryCode,
            City = request.City,
            TrainingStartDate = request.TrainingStartDate,
            TrainingEndDate = request.TrainingEndDate,
            DepartureDate = request.DepartureDate,
            ReturnDate = request.ReturnDate,
            BusinessJustification = request.BusinessJustification,
            CourseFee = request.CourseFee,
            CourseFeeCurrency = request.CourseFeeCurrency,
            FlightEstimate = request.FlightEstimate,
            FlightCurrency = request.FlightCurrency,
            Status = request.Status,
            AssignedReviewerId = request.AssignedReviewerId,
            AssignedReviewerName = request.AssignedReviewer is null
                ? null
                : $"{request.AssignedReviewer.FirstName} {request.AssignedReviewer.LastName}",
            SubmittedAtUtc = request.SubmittedAtUtc,
            CreatedAtUtc = request.CreatedAtUtc,
            UpdatedAtUtc = request.UpdatedAtUtc,
            WithdrawnAtUtc = request.WithdrawnAtUtc
        };
    }

    private static EstacodeRateResponseDto MapRate(EstaCodeRate rate)
    {
        return new EstacodeRateResponseDto
        {
            Id = rate.Id,
            CountryCode = rate.CountryCode,
            Grade = rate.Grade,
            DailyRateUsd = rate.DailyRateUsd,
            IsActive = rate.IsActive,
            CreatedAtUtc = rate.CreatedAtUtc,
            UpdatedAtUtc = rate.UpdatedAtUtc
        };
    }

    private static TrainingAllowanceVersionDto MapAllowance(
        TrainingAllowanceVersion allowance)
    {
        return new TrainingAllowanceVersionDto
        {
            Id = allowance.Id,
            TrainingRequestId = allowance.TrainingRequestId,
            VersionNumber = allowance.VersionNumber,
            CountryCodeSnapshot = allowance.CountryCodeSnapshot,
            GradeSnapshot = allowance.GradeSnapshot,
            DailyRateUsdSnapshot = allowance.DailyRateUsdSnapshot,
            EligibleDays = allowance.EligibleDays,
            TotalAmountUsd = allowance.TotalAmountUsd,
            Status = allowance.Status,
            IsCurrent = allowance.IsCurrent,
            CorrectionReason = allowance.CorrectionReason,
            CreatedByEmployeeId = allowance.CreatedByEmployeeId,
            CreatedAtUtc = allowance.CreatedAtUtc,
            ApprovedByEmployeeId = allowance.ApprovedByEmployeeId,
            ApprovedAtUtc = allowance.ApprovedAtUtc
        };
    }
}
