using EmployeeSelfServiceApi.DTOs.Payroll;
using EmployeeServiceApi.Enums;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Payroll;

public class PayslipItemValidator : AbstractValidator<PayslipItemDto>
{
    public PayslipItemValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .Must(type => type == PayslipItemType.Allowance
                          || type == PayslipItemType.Deduction)
            .WithMessage("Item type must be Allowance or Deduction.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .PrecisionScale(18, 2, false);
    }
}