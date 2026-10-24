using EmployeeSelfServiceApi.DTOs.KPI;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.KPI;

public class ReopenKpiPlanValidator
    : AbstractValidator<ReopenKpiPlanDto>
{
    public ReopenKpiPlanValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.NewDeadline)
            .Must(date => date != default)
            .WithMessage("A valid new deadline is required.");
    }
}