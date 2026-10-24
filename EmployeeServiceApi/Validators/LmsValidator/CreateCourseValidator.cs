using EmployeeSelfServiceApi.DTOs.LMS;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.LMS;

public class CreateCourseValidator : AbstractValidator<CreateCourseDto>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .When(x => x.Category is not null);
    }
}