using EmployeeSelfServiceApi.DTOs.Opportunities;
using EmployeeServiceApi.Enums;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Opportunities;

public class UpdateApplicationStatusValidator
    : AbstractValidator<UpdateApplicationStatusDto>
{
    public UpdateApplicationStatusValidator()
    {
        RuleFor(x => x.Status)
            .Must(status =>
                status == ApplicationStatus.Shortlisted ||
                status == ApplicationStatus.Selected ||
                status == ApplicationStatus.Rejected)
            .WithMessage(
                "Status must be Shortlisted, Selected, or Rejected.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000)
            .When(x => x.Comment is not null);
    }
}