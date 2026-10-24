
using EmployeeSelfServiceApi.DTOs.Opportunities;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Opportunities;

public class CreateOpportunityValidator
    : AbstractValidator<CreateOpportunityDto>
{
    public CreateOpportunityValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.Requirements)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.OpensAtUtc)
            .Must(BeUtcOrNull)
            .WithMessage("Opening date must be UTC.");

        RuleFor(x => x.ClosesAtUtc)
            .Must(BeUtcOrNull)
            .WithMessage("Closing date must be UTC.");

        RuleFor(x => x)
            .Must(x =>
                !x.OpensAtUtc.HasValue ||
                !x.ClosesAtUtc.HasValue ||
                x.ClosesAtUtc.Value > x.OpensAtUtc.Value)
            .WithMessage("Closing date must be after opening date.");

        RuleForEach(x => x.EligibleDepartments)
            .NotEmpty()
            .MaximumLength(100);

        RuleForEach(x => x.EligibleGrades)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.EligibleDepartments)
            .Must(HaveNoDuplicates)
            .WithMessage("Eligible departments must not contain duplicates.");

        RuleFor(x => x.EligibleGrades)
            .Must(HaveNoDuplicates)
            .WithMessage("Eligible grades must not contain duplicates.");
    }

    private static bool BeUtcOrNull(DateTime? value)
    {
        return !value.HasValue ||
               value.Value.Kind == DateTimeKind.Utc;
    }

    private static bool HaveNoDuplicates(List<string> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == values.Count;
    }
}
