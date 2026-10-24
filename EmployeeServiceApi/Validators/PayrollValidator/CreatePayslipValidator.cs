using EmployeeSelfServiceApi.DTOs.Payroll;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Payroll;

public class CreatePayslipValidator : AbstractValidator<CreatePayslipDto>
{
    public CreatePayslipValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0);

        RuleFor(x => x.PayrollMonth)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.PayrollYear)
            .InclusiveBetween(2000, 2100);

        RuleFor(x => x.BasicSalary)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.Items)
            .NotNull()
            .Must(items => items is not null && items.Count <= 100)
            .WithMessage("A payslip can contain at most 100 items.");

        RuleForEach(x => x.Items)
            .SetValidator(new PayslipItemValidator());
    }
}