using EmployeeServiceApi.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class WithdrawTrainingRequestValidator
    : AbstractValidator<WithdrawTrainingRequestDto>
{
    public WithdrawTrainingRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}