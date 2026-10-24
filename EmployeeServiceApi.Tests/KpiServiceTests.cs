using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.KPI;
using EmployeeSelfServiceApi.Services;
using EmployeeServiceApi.DTOs.KPI;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EmployeeServiceApi.Tests;

public class KpiServiceTests
{
    private static KpiService CreateService(ApplicationDbContext context) =>
        new(context, NullLogger<KpiService>.Instance);

    private static List<KpiTargetDto> SampleTargets() => new()
    {
        new KpiTargetDto { Title = "Deliveries", Description = "d", TargetValue = 4, Unit = "items", WeightPercentage = 20 },
        new KpiTargetDto { Title = "Quality", Description = "d", TargetValue = 100, Unit = "%", WeightPercentage = 80 }
    };

    private record Setup(
        ApplicationDbContext Context,
        KpiService Service,
        Employee Hr,
        Employee Manager,
        Employee Staff,
        int PeriodId);

    private static async Task<Setup> ArrangeAsync()
    {
        var context = TestDb.Create();
        var manager = await TestDb.AddEmployeeAsync(context, "Manager", roles: new[] { "Manager" });
        var hr = await TestDb.AddEmployeeAsync(context, "Hr", roles: new[] { "HR" });
        var staff = await TestDb.AddEmployeeAsync(context, "Staff", managerId: manager.Id);
        var period = await TestDb.AddOpenPeriodAsync(context);
        return new Setup(context, CreateService(context), hr, manager, staff, period.Id);
    }

    private static async Task<int> ActivePlanAsync(Setup s)
    {
        var plan = await s.Service.CreatePlanAsync(new CreateKpiPlanDto
        {
            EmployeeId = s.Staff.Id,
            ReviewPeriodId = s.PeriodId,
            Targets = SampleTargets()
        }, s.Hr.Id);

        await s.Service.ActivatePlanAsync(plan.Id, new ActivateKpiPlanDto(), s.Hr.Id);
        return plan.Id;
    }

    private static UpdateKpiProgressDto Progress(KpiPlanResponseDtoLike plan, decimal a, decimal b) => new()
    {
        Targets = new List<KpiProgressItemDto>
        {
            new() { TargetId = plan.TargetIds[0], ActualValue = a, EvidenceComment = "evidence" },
            new() { TargetId = plan.TargetIds[1], ActualValue = b, EvidenceComment = "evidence" }
        }
    };

    private record KpiPlanResponseDtoLike(int Id, List<int> TargetIds);

    private static async Task<KpiPlanResponseDtoLike> LoadAsync(Setup s, int planId)
    {
        var plan = await s.Service.GetPlanAsync(planId, s.Hr.Id);
        return new KpiPlanResponseDtoLike(plan.Id, plan.Targets.Select(t => t.Id).ToList());
    }

    // ---------------- Calculations ----------------

    [Fact]
    public void Achievement_target4_actual3_is_75_percent()
    {
        Assert.Equal(75m, KpiService.Achievement(3m, 4m));
    }

    [Fact]
    public void Achievement_is_capped_at_100()
    {
        Assert.Equal(100m, KpiService.Achievement(250m, 100m));
    }

    [Fact]
    public void Weighted_score_target4_actual3_weight20_is_15()
    {
        var overall = KpiService.CalculateOverallScore(new[] { (4m, 3m, 20m), (100m, 100m, 80m) });

        // 15.00 from the first KPI + 80.00 from the second.
        Assert.Equal(95.00m, overall);
    }

    [Fact]
    public void Overall_score_rounds_away_from_zero_to_two_places()
    {
        // 1/3 of 100 = 33.333...  -> 33.33 ; 2/3 -> 66.666... -> 66.67
        Assert.Equal(33.33m, KpiService.CalculateOverallScore(new[] { (3m, 1m, 100m) }));
        Assert.Equal(66.67m, KpiService.CalculateOverallScore(new[] { (3m, 2m, 100m) }));
        // exact midpoint: 0.125 -> 0.13 (AwayFromZero, not banker's rounding)
        Assert.Equal(0.13m, KpiService.CalculateOverallScore(new[] { (800m, 1m, 100m) }));
    }

    // ---------------- Creation / activation ----------------

    [Fact]
    public async Task Hr_cannot_create_a_plan_for_themselves()
    {
        var s = await ArrangeAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.CreatePlanAsync(new CreateKpiPlanDto
            {
                EmployeeId = s.Hr.Id,
                ReviewPeriodId = s.PeriodId,
                Targets = SampleTargets()
            }, s.Hr.Id));
    }

    [Fact]
    public async Task Staff_cannot_create_plans()
    {
        var s = await ArrangeAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.CreatePlanAsync(new CreateKpiPlanDto
            {
                EmployeeId = s.Staff.Id,
                ReviewPeriodId = s.PeriodId,
                Targets = SampleTargets()
            }, s.Staff.Id));
    }

    [Fact]
    public async Task Duplicate_plan_for_same_employee_and_period_is_rejected()
    {
        var s = await ArrangeAsync();
        var request = new CreateKpiPlanDto
        {
            EmployeeId = s.Staff.Id,
            ReviewPeriodId = s.PeriodId,
            Targets = SampleTargets()
        };

        await s.Service.CreatePlanAsync(request, s.Hr.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.CreatePlanAsync(request, s.Hr.Id));
    }

    [Fact]
    public async Task Activation_requires_weights_totalling_exactly_100()
    {
        var s = await ArrangeAsync();

        var plan = await s.Service.CreatePlanAsync(new CreateKpiPlanDto
        {
            EmployeeId = s.Staff.Id,
            ReviewPeriodId = s.PeriodId,
            Targets = new List<KpiTargetDto>
            {
                new() { Title = "A", Description = "d", TargetValue = 1, Unit = "u", WeightPercentage = 60 },
                new() { Title = "B", Description = "d", TargetValue = 1, Unit = "u", WeightPercentage = 39.99m }
            }
        }, s.Hr.Id);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            s.Service.ActivatePlanAsync(plan.Id, new ActivateKpiPlanDto(), s.Hr.Id));
    }

    [Fact]
    public async Task Activation_defaults_reviewer_to_reporting_manager()
    {
        var s = await ArrangeAsync();
        var planId = await ActivePlanAsync(s);

        var plan = await s.Service.GetPlanAsync(planId, s.Hr.Id);

        Assert.Equal(KpiPlanStatus.Active, plan.Status);
        Assert.Equal(s.Manager.Id, plan.AssignedReviewerId);
    }

    [Fact]
    public async Task Targets_cannot_be_edited_after_activation()
    {
        var s = await ArrangeAsync();
        var planId = await ActivePlanAsync(s);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.UpdatePlanAsync(planId, new UpdateKpiPlanDto { Targets = SampleTargets() }, s.Hr.Id));
    }

    // ---------------- Progress / submission ----------------

    [Fact]
    public async Task Owner_can_record_progress_but_others_cannot()
    {
        var s = await ArrangeAsync();
        var other = await TestDb.AddEmployeeAsync(s.Context, "Other", managerId: s.Manager.Id);
        var planId = await ActivePlanAsync(s);
        var plan = await LoadAsync(s, planId);

        var updated = await s.Service.UpdateProgressAsync(planId, Progress(plan, 3, 90), s.Staff.Id);
        Assert.Equal(3m, updated.Targets[0].ActualValue);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.UpdateProgressAsync(planId, Progress(plan, 1, 1), other.Id));
    }

    [Fact]
    public async Task Negative_actual_values_are_rejected()
    {
        var s = await ArrangeAsync();
        var planId = await ActivePlanAsync(s);
        var plan = await LoadAsync(s, planId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            s.Service.UpdateProgressAsync(planId, Progress(plan, -1, 5), s.Staff.Id));
    }

    [Fact]
    public async Task Submitted_plan_is_locked_for_the_owner()
    {
        var s = await ArrangeAsync();
        var planId = await ActivePlanAsync(s);
        var plan = await LoadAsync(s, planId);

        await s.Service.UpdateProgressAsync(planId, Progress(plan, 3, 90), s.Staff.Id);
        await s.Service.SubmitPlanAsync(planId, s.Staff.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.UpdateProgressAsync(planId, Progress(plan, 4, 100), s.Staff.Id));

        // submitting twice is an invalid transition
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.SubmitPlanAsync(planId, s.Staff.Id));
    }

    [Fact]
    public async Task Edits_after_the_deadline_are_rejected()
    {
        var s = await ArrangeAsync();
        var planId = await ActivePlanAsync(s);
        var plan = await LoadAsync(s, planId);

        var entity = await s.Context.KpiPlans.FirstAsync(p => p.Id == planId);
        entity.SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        await s.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.UpdateProgressAsync(planId, Progress(plan, 3, 90), s.Staff.Id));
    }

    // ---------------- Review ----------------

    private static async Task<int> SubmittedPlanAsync(Setup s)
    {
        var planId = await ActivePlanAsync(s);
        var plan = await LoadAsync(s, planId);
        await s.Service.UpdateProgressAsync(planId, Progress(plan, 3, 100), s.Staff.Id);
        await s.Service.SubmitPlanAsync(planId, s.Staff.Id);
        return planId;
    }

    [Fact]
    public async Task Only_the_assigned_reviewer_can_review()
    {
        var s = await ArrangeAsync();
        var otherManager = await TestDb.AddEmployeeAsync(s.Context, "Mgr2", roles: new[] { "Manager" });
        var planId = await SubmittedPlanAsync(s);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            s.Service.ReviewPlanAsync(planId,
                new ReviewKpiPlanDto { Decision = KpiReviewDecision.Approve }, otherManager.Id));
    }

    [Fact]
    public async Task Approval_finalizes_with_server_calculated_score()
    {
        var s = await ArrangeAsync();
        var planId = await SubmittedPlanAsync(s);

        var result = await s.Service.ReviewPlanAsync(planId,
            new ReviewKpiPlanDto { Decision = KpiReviewDecision.Approve }, s.Manager.Id);

        Assert.Equal(KpiPlanStatus.Finalized, result.Status);
        // target 4 / actual 3 / weight 20 = 15.00 ; 100/100 weight 80 = 80.00
        Assert.Equal(95.00m, result.FinalScorePercentage);
        Assert.Equal(75m, result.Targets[0].ScorePercentage);
        Assert.Equal(15.00m, result.Targets[0].WeightedScore);
    }

    [Fact]
    public async Task Returning_requires_a_comment_and_unlocks_the_plan()
    {
        var s = await ArrangeAsync();
        var planId = await SubmittedPlanAsync(s);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            s.Service.ReviewPlanAsync(planId,
                new ReviewKpiPlanDto { Decision = KpiReviewDecision.ReturnToEmployee }, s.Manager.Id));

        var returned = await s.Service.ReviewPlanAsync(planId,
            new ReviewKpiPlanDto { Decision = KpiReviewDecision.ReturnToEmployee, ReviewerComment = "Add evidence" },
            s.Manager.Id);

        Assert.Equal(KpiPlanStatus.Returned, returned.Status);
    }

    [Fact]
    public async Task Finalized_plan_cannot_be_edited_or_reopened()
    {
        var s = await ArrangeAsync();
        var planId = await SubmittedPlanAsync(s);
        var plan = await LoadAsync(s, planId);

        await s.Service.ReviewPlanAsync(planId,
            new ReviewKpiPlanDto { Decision = KpiReviewDecision.Approve }, s.Manager.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.UpdateProgressAsync(planId, Progress(plan, 4, 100), s.Staff.Id));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.ReopenPlanAsync(planId, new ReopenKpiPlanDto
            {
                Reason = "x",
                NewDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10)
            }, s.Hr.Id));
    }

    // ---------------- Reopen ----------------

    [Fact]
    public async Task Reopen_needs_a_future_deadline_and_keeps_the_original()
    {
        var s = await ArrangeAsync();
        var planId = await ActivePlanAsync(s);
        var original = (await s.Service.GetPlanAsync(planId, s.Hr.Id)).SubmissionDeadline;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            s.Service.ReopenPlanAsync(planId, new ReopenKpiPlanDto
            {
                Reason = "late",
                NewDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
            }, s.Hr.Id));

        var newDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);
        var reopened = await s.Service.ReopenPlanAsync(planId, new ReopenKpiPlanDto
        {
            Reason = "extension",
            NewDeadline = newDeadline
        }, s.Hr.Id);

        Assert.Equal(original, reopened.SubmissionDeadline);
        Assert.Equal(newDeadline, reopened.RevisedDeadline);
        Assert.Equal(1, await s.Context.KpiReopeningHistories.CountAsync());
    }

    [Fact]
    public async Task Submitted_plan_must_be_returned_before_reopening()
    {
        var s = await ArrangeAsync();
        var planId = await SubmittedPlanAsync(s);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            s.Service.ReopenPlanAsync(planId, new ReopenKpiPlanDto
            {
                Reason = "x",
                NewDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10)
            }, s.Hr.Id));
    }
}
