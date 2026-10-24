using EmployeeSelfServiceApi.DTOs.Payroll;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Payroll;

public class RevisePayslipValidator : AbstractValidator<RevisePayslipDto>
{
    public RevisePayslipValidator()
    {
        RuleFor(x => x.BasicSalary)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.Items)
            .NotNull()
            .Must(items => items is not null && items.Count <= 100)
            .WithMessage("A payslip can contain at most 100 items.");

        RuleForEach(x => x.Items)
            .SetValidator(new PayslipItemValidator());

        RuleFor(x => x.RevisionReason)
            .NotEmpty()
            .MaximumLength(1000)
            .WithMessage("A reason is required for every payslip revision.");
    }
}