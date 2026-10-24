using EmployeeServiceApi.DTOs.Common;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Common;

public class PaginationRequestValidator
    : AbstractValidator<PaginationRequestDto>
{
    public PaginationRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
