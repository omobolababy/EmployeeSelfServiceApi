using EmployeeSelfServiceApi.DTOs.Training;
using EmployeeServiceApi.Enums;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class TrainingDecisionValidator
    : AbstractValidator<TrainingDecisionDto>
{
    public TrainingDecisionValidator()
    {
        RuleFor(x => x.Decision)
            .IsInEnum()
            .Must(decision => Enum.IsDefined(typeof(TrainingDecision), decision))
            .WithMessage("A valid training decision is required.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => x.Comment is not null);

        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .When(x => x.Reason is not null);

        RuleFor(x => x.Decision)
            .Must(decision => decision != TrainingDecision.Reject
                              || !string.IsNullOrWhiteSpace(
                                  decision.ToString()))
            .WithMessage("A valid decision is required.");

        RuleFor(x => x.ReassignReviewerId)
            .GreaterThan(0)
            .When(x => x.ReassignReviewerId.HasValue);
    }
}