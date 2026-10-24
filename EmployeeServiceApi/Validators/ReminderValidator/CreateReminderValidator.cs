using EmployeeSelfServiceApi.DTOs.Reminders;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Reminders;

public class CreateReminderValidator
    : AbstractValidator<CreateReminderDto>
{
    public CreateReminderValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.ScheduledAtUtc)
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Scheduled time must be UTC.");

        RuleFor(x => x.RelatedEntityType)
            .MaximumLength(100)
            .When(x => x.RelatedEntityType is not null);

        RuleFor(x => x.RelatedEntityId)
            .GreaterThan(0)
            .When(x => x.RelatedEntityId.HasValue);

        RuleFor(x => x.DeduplicationKey)
            .NotEmpty()
            .MaximumLength(250);
    }
}