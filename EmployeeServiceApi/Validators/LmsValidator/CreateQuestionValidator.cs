using EmployeeSelfServiceApi.DTOs.LMS;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.LMS;

public class CreateQuestionValidator : AbstractValidator<CreateQuestionDto>
{
    public CreateQuestionValidator()
    {
        RuleFor(x => x.QuestionText)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.OrderNumber)
            .GreaterThan(0);

        RuleFor(x => x.Marks)
            .GreaterThan(0)
            .PrecisionScale(8, 2, false);

        RuleFor(x => x.Options)
            .NotNull()
            .Must(options => options is { Count: >= 2 and <= 6 })
            .WithMessage("Each question must have between 2 and 6 options.");

        RuleForEach(x => x.Options)
            .ChildRules(option =>
            {
                option.RuleFor(x => x.OptionText)
                    .NotEmpty()
                    .MaximumLength(500);
            });

        RuleFor(x => x.Options)
            .Must(options => options is not null
                             && options.Count(x => x.IsCorrect) == 1)
            .WithMessage("Each question must have exactly one correct option.");

        RuleFor(x => x.Options)
            .Must(options => options is not null
                             && options.Select(x => x.OptionText.Trim())
                                 .Distinct(StringComparer.OrdinalIgnoreCase).Count()
                             == options.Count)
            .WithMessage("Duplicate answer options are not allowed.");
    }
}