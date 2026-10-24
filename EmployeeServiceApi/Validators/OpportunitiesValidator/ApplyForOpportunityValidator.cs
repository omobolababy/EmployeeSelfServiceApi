
using EmployeeSelfServiceApi.DTOs.Opportunities;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Opportunities;

public class ApplyForOpportunityValidator
    : AbstractValidator<ApplyForOpportunityDto>
{
    public ApplyForOpportunityValidator()
    {
        RuleFor(x => x.CoverLetter)
            .MaximumLength(5000)
            .When(x => x.CoverLetter is not null);

        RuleFor(x => x.AttachmentId)
            .GreaterThan(0)
            .When(x => x.AttachmentId.HasValue);
    }
}
