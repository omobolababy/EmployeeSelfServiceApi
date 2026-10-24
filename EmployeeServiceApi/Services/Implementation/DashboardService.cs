
using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Dashboard;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.KPI;
using EmployeeServiceApi.Entities.JobApplication;
using EmployeeServiceApi.Entities.Payslip;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            var employee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new EmployeeDashboardDto
                {
                    EmployeeName = e.FirstName + " " + e.LastName,
                    EmployeeNumber = e.EmployeeNumber,
                    Department = e.Department,
                    Grade = e.Grade
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (employee is null)
            {
                throw new KeyNotFoundException(
                    $"Employee with ID {employeeId} was not found.");
            }

            employee.UnreadNotifications =
                await _context.Notifications.CountAsync(
                    n => n.EmployeeId == employeeId &&
                         n.ReadAtUtc == null,
                    cancellationToken);

            employee.PendingTrainingRequests =
                await _context.TrainingRequests.CountAsync(
                    t => t.EmployeeId == employeeId &&
                         (t.Status == TrainingStatus.PendingManagerApproval ||
                          t.Status == TrainingStatus.PendingHRApproval ||
                          t.Status == TrainingStatus.PendingFinanceApproval ||
                          t.Status == TrainingStatus.ReturnedToHR),
                    cancellationToken);

            employee.AssignedCourses =
                await _context.CourseAssignments.CountAsync(
                    a => a.EmployeeId == employeeId,
                    cancellationToken);

            employee.IncompleteCourses =
                await _context.CourseAssignments.CountAsync(
                    a => a.EmployeeId == employeeId &&
                         a.Status != CourseAssignmentStatus.Completed,
                    cancellationToken);

            employee.PendingKpiPlans =
                await _context.KpiPlans.CountAsync(
                    p => p.EmployeeId == employeeId &&
                         (p.Status == KpiPlanStatus.Active ||
                          p.Status == KpiPlanStatus.Returned),
                    cancellationToken);

            employee.PublishedPayslips =
                await _context.Payslips.CountAsync(
                    p => p.EmployeeId == employeeId &&
                         p.CurrentVersionId.HasValue &&
                         p.Versions.Any(v =>
                             v.Id == p.CurrentVersionId.Value &&
                             v.Status == PayslipStatus.Published),
                    cancellationToken);

            employee.OpenOpportunities =
                await _context.Opportunities.CountAsync(
                    o => o.Status == OpportunityStatus.Published &&
                         (!o.OpensAtUtc.HasValue ||
                          o.OpensAtUtc.Value <= now) &&
                         (!o.ClosesAtUtc.HasValue ||
                          o.ClosesAtUtc.Value >= now),
                    cancellationToken);

            employee.SubmittedApplications =
                await _context.JobApplications.CountAsync(
                    a => a.EmployeeId == employeeId &&
                         a.Status == ApplicationStatus.Submitted,
                    cancellationToken);

            return employee;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate employee dashboard for {EmployeeId}",
                employeeId);

            throw;
        }
    }

    public async Task<ManagerDashboardDto> GetManagerDashboardAsync(
        int managerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var managerExists = await _context.Employees
                .AnyAsync(e => e.Id == managerId, cancellationToken);

            if (!managerExists)
            {
                throw new KeyNotFoundException(
                    $"Manager with ID {managerId} was not found.");
            }

            return new ManagerDashboardDto
            {
                TeamSize = await _context.Employees.CountAsync(
                    e => e.ReportingManagerId == managerId,
                    cancellationToken),

                PendingTrainingApprovals =
                    await _context.TrainingRequests.CountAsync(
                        t => t.AssignedReviewerId == managerId &&
                             t.Status == TrainingStatus.PendingManagerApproval,
                        cancellationToken),

                PendingKpiReviews =
                    await _context.KpiPlans.CountAsync(
                        p => p.AssignedReviewerId == managerId &&
                             p.EmployeeId != managerId &&
                             p.Status == KpiPlanStatus.Submitted,
                        cancellationToken),

                TeamCoursesIncomplete =
                    await _context.CourseAssignments.CountAsync(
                        a => a.Employee.ReportingManagerId == managerId &&
                             a.Status != CourseAssignmentStatus.Completed,
                        cancellationToken),

                UnreadNotifications =
                    await _context.Notifications.CountAsync(
                        n => n.EmployeeId == managerId &&
                             n.ReadAtUtc == null,
                        cancellationToken)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate manager dashboard for {ManagerId}",
                managerId);

            throw;
        }
    }

    public async Task<HrDashboardDto> GetHrDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            return new HrDashboardDto
            {
                ActiveEmployees =
                    await _context.Employees.CountAsync(
                        e => e.Status == EmployeeStatus.Active,
                        cancellationToken),

                PendingActivations =
                    await _context.Employees.CountAsync(
                        e => e.Status == EmployeeStatus.PendingActivation,
                        cancellationToken),

                PendingTrainingApprovals =
                    await _context.TrainingRequests.CountAsync(
                        t => t.Status == TrainingStatus.PendingHRApproval ||
                             t.Status == TrainingStatus.ReturnedToHR,
                        cancellationToken),

                PublishedCourses =
                    await _context.Courses.CountAsync(
                        c => c.Status == CourseStatus.Published,
                        cancellationToken),

                OpenOpportunities =
                    await _context.Opportunities.CountAsync(
                        o => o.Status == OpportunityStatus.Published &&
                             (!o.OpensAtUtc.HasValue ||
                              o.OpensAtUtc.Value <= now) &&
                             (!o.ClosesAtUtc.HasValue ||
                              o.ClosesAtUtc.Value >= now),
                        cancellationToken),

                PendingApplications =
                    await _context.JobApplications.CountAsync(
                        a => a.Status == ApplicationStatus.Submitted,
                        cancellationToken)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate HR dashboard");

            throw;
        }
    }

    public async Task<FinanceDashboardDto> GetFinanceDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return new FinanceDashboardDto
            {
                PendingTrainingApprovals =
                    await _context.TrainingRequests.CountAsync(
                        t => t.Status == TrainingStatus.PendingFinanceApproval,
                        cancellationToken),

                DraftPayslips =
                    await _context.Payslips.CountAsync(
                        p => p.CurrentVersionId.HasValue &&
                             p.Versions.Any(v =>
                                 v.Id == p.CurrentVersionId.Value &&
                                 v.Status == PayslipStatus.Draft),
                        cancellationToken),

                PublishedPayslips =
                    await _context.Payslips.CountAsync(
                        p => p.CurrentVersionId.HasValue &&
                             p.Versions.Any(v =>
                                 v.Id == p.CurrentVersionId.Value &&
                                 v.Status == PayslipStatus.Published),
                        cancellationToken),

                FailedEmails =
                    await _context.EmailQueueItems.CountAsync(
                        e => e.Status == EmailMessageStatus.Failed,
                        cancellationToken)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate finance dashboard");

            throw;
        }
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return new AdminDashboardDto
            {
                TotalEmployees =
                    await _context.Employees.CountAsync(
                        cancellationToken),

                ActiveEmployees =
                    await _context.Employees.CountAsync(
                        e => e.Status == EmployeeStatus.Active,
                        cancellationToken),

                InactiveEmployees =
                    await _context.Employees.CountAsync(
                        e => e.Status == EmployeeStatus.Inactive,
                        cancellationToken),

                PendingActivations =
                    await _context.Employees.CountAsync(
                        e => e.Status == EmployeeStatus.PendingActivation,
                        cancellationToken),

                PendingEmailMessages =
                    await _context.EmailQueueItems.CountAsync(
                        e => e.Status == EmailMessageStatus.Pending ||
                             e.Status == EmailMessageStatus.Processing,
                        cancellationToken),

                FailedEmailMessages =
                    await _context.EmailQueueItems.CountAsync(
                        e => e.Status == EmailMessageStatus.Failed,
                        cancellationToken),

                TotalTrainingRequests =
                    await _context.TrainingRequests.CountAsync(
                        cancellationToken),

                TotalCourseAssignments =
                    await _context.CourseAssignments.CountAsync(
                        cancellationToken),

                TotalKpiPlans =
                    await _context.KpiPlans.CountAsync(
                        cancellationToken),

                TotalPayslips =
                    await _context.Payslips.CountAsync(
                        cancellationToken),

                TotalApplications =
                    await _context.JobApplications.CountAsync(
                        cancellationToken)
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate admin dashboard");

            throw;
        }
    }
}
