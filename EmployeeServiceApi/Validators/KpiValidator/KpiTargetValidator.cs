using EmployeeSelfServiceApi.DTOs.KPI;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.KPI;

public class KpiTargetValidator : AbstractValidator<KpiTargetDto>
{
    public KpiTargetValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.TargetValue)
            .GreaterThan(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.WeightPercentage)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .PrecisionScale(5, 2, false);
    }
}