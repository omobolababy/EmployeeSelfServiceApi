using EmployeeSelfServiceApi.DTOs.LMS;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.LMS;

public class AssignCourseValidator : AbstractValidator<AssignCourseDto>
{
    public AssignCourseValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0);

        RuleFor(x => x.DueAtUtc)
            .Must(date => !date.HasValue || date.Value.Kind == DateTimeKind.Utc)
            .WithMessage("Due date must be supplied in UTC.");
    }
}