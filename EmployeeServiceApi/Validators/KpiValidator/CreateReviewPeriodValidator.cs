using EmployeeSelfServiceApi.DTOs.KPI;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.KPI;

public class CreateReviewPeriodValidator
    : AbstractValidator<CreateReviewPeriodDto>
{
    public CreateReviewPeriodValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date cannot be before the start date.");

        RuleFor(x => x.SubmissionDeadline)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Submission deadline cannot be before the start date.");
    }
}