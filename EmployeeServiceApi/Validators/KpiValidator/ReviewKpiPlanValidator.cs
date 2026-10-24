using EmployeeSelfServiceApi.DTOs.KPI;
using EmployeeServiceApi.Enums;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.KPI;

public class ReviewKpiPlanValidator : AbstractValidator<ReviewKpiPlanDto>
{
    public ReviewKpiPlanValidator()
    {
        RuleFor(x => x.Decision)
            .IsInEnum()
            .Must(x => x == KpiReviewDecision.Approve ||
                       x == KpiReviewDecision.ReturnToEmployee)
            .WithMessage("Select a valid review decision.");

        RuleFor(x => x.ReviewerComment)
            .MaximumLength(2000)
            .When(x => x.ReviewerComment is not null);

        RuleFor(x => x.ReviewerComment)
            .NotEmpty()
            .When(x => x.Decision == KpiReviewDecision.ReturnToEmployee)
            .WithMessage("A comment is required when returning a plan.");

        RuleForEach(x => x.Targets).ChildRules(target =>
        {
            target.RuleFor(x => x.TargetId).GreaterThan(0);
            target.RuleFor(x => x.ReviewerComment)
                .MaximumLength(2000)
                .When(x => x.ReviewerComment is not null);
        });
    }
}
