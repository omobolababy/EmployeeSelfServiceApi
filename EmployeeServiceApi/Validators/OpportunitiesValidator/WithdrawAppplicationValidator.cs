
using EmployeeSelfServiceApi.DTOs.Opportunities;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Opportunities;

public class WithdrawApplicationValidator
    : AbstractValidator<WithdrawApplicationDto>
{
    public WithdrawApplicationValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
