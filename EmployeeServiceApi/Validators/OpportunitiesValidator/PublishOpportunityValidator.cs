using EmployeeSelfServiceApi.DTOs.Opportunities;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Opportunities;

public class PublishOpportunityValidator
    : AbstractValidator<PublishOpportunityDto>
{
    public PublishOpportunityValidator()
    {
        RuleFor(x => x.ConfirmPublish)
            .Equal(true)
            .WithMessage("You must confirm before publishing the opportunity.");
    }
}