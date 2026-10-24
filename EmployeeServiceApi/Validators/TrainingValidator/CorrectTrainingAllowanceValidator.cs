using EmployeeServiceApi.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class CorrectTrainingAllowanceValidator
    : AbstractValidator<CorrectTrainingAllowanceDto>
{
    public CorrectTrainingAllowanceValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage(
                "A correction reason is required and must not exceed 1000 characters.");
    }
}