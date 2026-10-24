using EmployeeSelfServiceApi.DTOs.KPI;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.KPI;

public class UpdateKpiProgressValidator : AbstractValidator<UpdateKpiProgressDto>
{
    public UpdateKpiProgressValidator()
    {
        RuleFor(x => x.Targets)
            .NotEmpty()
            .WithMessage("At least one target update is required.");

        RuleForEach(x => x.Targets).ChildRules(item =>
        {
            item.RuleFor(x => x.TargetId).GreaterThan(0);

            item.RuleFor(x => x.ActualValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Actual values cannot be negative.")
                .PrecisionScale(18, 2, false);

            item.RuleFor(x => x.EvidenceComment)
                .MaximumLength(2000)
                .When(x => x.EvidenceComment is not null);
        });

        RuleFor(x => x.Targets)
            .Must(t => t.Select(i => i.TargetId).Distinct().Count() == t.Count)
            .WithMessage("A target cannot be updated more than once in one request.");
    }
}
