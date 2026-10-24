using EmployeeSelfServiceApi.DTOs.LMS;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.LMS;

public class UpdateLessonValidator : AbstractValidator<UpdateLessonDto>
{
    public UpdateLessonValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(20000);

        RuleFor(x => x.VideoUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
            .WithMessage("Video URL must be a valid absolute HTTP or HTTPS URL.");

        RuleFor(x => x.OrderNumber)
            .GreaterThan(0);
    }

    private static bool BeAValidUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp
                   || uri.Scheme == Uri.UriSchemeHttps);
    }
}