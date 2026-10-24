using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Opportunities;
using EmployeeSelfServiceApi.Services;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EmployeeServiceApi.Tests;

public class OpportunityServiceTests
{
    private record Setup(ApplicationDbContext Context, OpportunityService Service, Employee Hr, Employee Staff);

    private static async Task<Setup> ArrangeAsync()
    {
        var context = TestDb.Create();
        var hr = await TestDb.AddEmployeeAsync(context, "Hr", roles: new[] { "HR" });
        var staff = await TestDb.AddEmployeeAsync(context, "Staff", "Engineering", "G2");
        return new Setup(context, new OpportunityService(context, NullLogger<OpportunityService>.Instance), hr, staff);
    }

    private static CreateOpportunityDto NewOpportunity(
        DateTime? closes = null,
        List<string>? departments = null,
        List<string>? grades = null) => new()
    {
        Title = "Senior Engineer",
        Description = "desc",
        Requirements = "req",
        Type = OpportunityType.InternalVacancy,
        ClosesAtUtc = closes ?? DateTime.UtcNow.AddDays(7),
        EligibleDepartments = departments ?? new List<string>(),
        EligibleGrades = grades ?? new List<string>()
    };

    private static async Task<int> PublishedAsync(Setup s, CreateOpportunityDto? dto = null)
    {
        var created = await s.Service.CreateOpportunityAsync(dto ?? NewOpportunity(), s.Hr.Id);
        await s.Service.PublishOpportunityAsync(created.Id, new PublishOpportunityDto(), s.Hr.Id);
        return created.Id;
    }

    [Fact]
    public async Task Publishing_requires_a_future_closing_time()
    {
        var s = await ArrangeAsync();
        var created = await s.Service.CreateOpportunityAsync(
            NewOpportunity(closes: DateTime.UtcNow.AddDays(-1)), s.Hr.Id);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            s.Service.PublishOpportunityAsync(created.Id, new PublishOpportunityDto(), s.Hr.Id));
    }

    [Fact]
    public async Task Only_hr_can_create_opportunities()
    {
        var s = await ArrangeAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.CreateOpportunityAsync(NewOpportunity(), s.Staff.Id));
    }

    [Fact]
    public async Task Staff_cannot_see_draft_opportunities()
    {
        var s = await ArrangeAsync();
        var created = await s.Service.CreateOpportunityAsync(NewOpportunity(), s.Hr.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            s.Service.GetOpportunityByIdAsync(created.Id, s.Staff.Id));

        // HR sees it.
        var seen = await s.Service.GetOpportunityByIdAsync(created.Id, s.Hr.Id);
        Assert.Equal(OpportunityStatus.Draft, seen.Status);
    }

    [Fact]
    public async Task Duplicate_application_is_rejected()
    {
        var s = await ArrangeAsync();
        var id = await PublishedAsync(s);

        await s.Service.ApplyForOpportunityAsync(id, new ApplyForOpportunityDto { CoverLetter = "hi" }, s.Staff.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.ApplyForOpportunityAsync(id, new ApplyForOpportunityDto { CoverLetter = "again" }, s.Staff.Id));
    }

    [Fact]
    public async Task Applications_are_blocked_after_the_closing_time_even_if_not_closed_by_hr()
    {
        var s = await ArrangeAsync();
        var id = await PublishedAsync(s);

        var entity = await s.Context.Opportunities.FindAsync(id);
        entity!.ClosesAtUtc = DateTime.UtcNow.AddMinutes(-5);
        await s.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.ApplyForOpportunityAsync(id, new ApplyForOpportunityDto { CoverLetter = "late" }, s.Staff.Id));
    }

    [Fact]
    public async Task Ineligible_department_is_rejected_and_matching_department_and_grade_accepted()
    {
        var s = await ArrangeAsync();
        var restricted = await PublishedAsync(s, NewOpportunity(
            departments: new List<string> { "Finance" }));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.ApplyForOpportunityAsync(restricted, new ApplyForOpportunityDto { CoverLetter = "x" }, s.Staff.Id));

        var matching = await PublishedAsync(s, NewOpportunity(
            departments: new List<string> { "Engineering" },
            grades: new List<string> { "G2" }));

        var application = await s.Service.ApplyForOpportunityAsync(
            matching, new ApplyForOpportunityDto { CoverLetter = "x" }, s.Staff.Id);

        // The eligibility snapshot is stored with the application.
        Assert.Equal("Engineering", application.DepartmentSnapshot);
        Assert.Equal("G2", application.GradeSnapshot);
    }

    [Fact]
    public async Task Status_transitions_follow_the_allowed_paths()
    {
        var s = await ArrangeAsync();
        var id = await PublishedAsync(s);
        var application = await s.Service.ApplyForOpportunityAsync(
            id, new ApplyForOpportunityDto { CoverLetter = "x" }, s.Staff.Id);

        // Submitted -> Selected is not allowed.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.UpdateApplicationStatusAsync(application.Id,
                new UpdateApplicationStatusDto { Status = ApplicationStatus.Selected }, s.Hr.Id));

        await s.Service.UpdateApplicationStatusAsync(application.Id,
            new UpdateApplicationStatusDto { Status = ApplicationStatus.Shortlisted }, s.Hr.Id);

        var selected = await s.Service.UpdateApplicationStatusAsync(application.Id,
            new UpdateApplicationStatusDto { Status = ApplicationStatus.Selected }, s.Hr.Id);

        Assert.Equal(ApplicationStatus.Selected, selected.Status);

        // Selected is final.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.UpdateApplicationStatusAsync(application.Id,
                new UpdateApplicationStatusDto { Status = ApplicationStatus.Rejected }, s.Hr.Id));
    }

    [Fact]
    public async Task Non_hr_cannot_change_application_status()
    {
        var s = await ArrangeAsync();
        var id = await PublishedAsync(s);
        var application = await s.Service.ApplyForOpportunityAsync(
            id, new ApplyForOpportunityDto { CoverLetter = "x" }, s.Staff.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.UpdateApplicationStatusAsync(application.Id,
                new UpdateApplicationStatusDto { Status = ApplicationStatus.Shortlisted }, s.Staff.Id));
    }

    [Fact]
    public async Task Applicant_can_withdraw_submitted_but_not_selected_applications()
    {
        var s = await ArrangeAsync();
        var id = await PublishedAsync(s);
        var application = await s.Service.ApplyForOpportunityAsync(
            id, new ApplyForOpportunityDto { CoverLetter = "x" }, s.Staff.Id);

        await s.Service.UpdateApplicationStatusAsync(application.Id,
            new UpdateApplicationStatusDto { Status = ApplicationStatus.Shortlisted }, s.Hr.Id);
        await s.Service.UpdateApplicationStatusAsync(application.Id,
            new UpdateApplicationStatusDto { Status = ApplicationStatus.Selected }, s.Hr.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.WithdrawApplicationAsync(application.Id,
                new WithdrawApplicationDto { Reason = "changed my mind" }, s.Staff.Id));
    }

    [Fact]
    public async Task Another_employee_cannot_view_my_application()
    {
        var s = await ArrangeAsync();
        var other = await TestDb.AddEmployeeAsync(s.Context, "Other");
        var id = await PublishedAsync(s);
        var application = await s.Service.ApplyForOpportunityAsync(
            id, new ApplyForOpportunityDto { CoverLetter = "x" }, s.Staff.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.GetApplicationByIdAsync(application.Id, other.Id));
    }
}
