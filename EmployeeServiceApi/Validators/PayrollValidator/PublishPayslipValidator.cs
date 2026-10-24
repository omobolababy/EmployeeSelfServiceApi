using EmployeeServiceApi.DTOs.Payroll;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Payroll;

public class PublishPayslipValidator : AbstractValidator<PublishPayslipDto>
{
    public PublishPayslipValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => x.Comment is not null);
    }
}