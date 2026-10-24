using EmployeeSelfServiceApi.DTOs.LMS;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.LMS;

public class SubmitAssessmentValidator
    : AbstractValidator<SubmitAssessmentDto>
{
    public SubmitAssessmentValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull()
            .Must(answers => answers is { Count: > 0 })
            .WithMessage("At least one answer is required.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(x => x.QuestionId)
                .GreaterThan(0);

            answer.RuleFor(x => x.SelectedOptionId)
                .GreaterThan(0);
        });

        RuleFor(x => x.Answers)
            .Must(answers => answers is not null
                             && answers.Select(x => x.QuestionId).Distinct().Count()
                             == answers.Count)
            .WithMessage("A question cannot be answered more than once.");
    }
}