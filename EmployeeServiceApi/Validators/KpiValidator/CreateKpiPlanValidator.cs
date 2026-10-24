using EmployeeSelfServiceApi.DTOs.KPI;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.KPI;

public class CreateKpiPlanValidator : AbstractValidator<CreateKpiPlanDto>
{
    public CreateKpiPlanValidator()
    {
        RuleFor(x => x.ReviewPeriodId)
            .GreaterThan(0);

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0);

        RuleFor(x => x.Targets)
            .NotNull()
            .Must(targets => targets is { Count: > 0 })
            .WithMessage("At least one KPI target is required.");

        RuleForEach(x => x.Targets)
            .SetValidator(new KpiTargetValidator());

        RuleFor(x => x.Targets)
            .Must(targets => targets is not null
                             && targets.Count == targets
                                 .Select(t => t.Title.Trim())
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Count())
            .WithMessage("KPI target titles must be unique.");

    }
}