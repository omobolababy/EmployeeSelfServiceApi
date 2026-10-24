using EmployeeServiceApi.DTOs.EmployeeDto;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Employees;

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Department)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Grade)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.ReportingManagerId)
            .GreaterThan(0)
            .When(x => x.ReportingManagerId.HasValue);
    }
}