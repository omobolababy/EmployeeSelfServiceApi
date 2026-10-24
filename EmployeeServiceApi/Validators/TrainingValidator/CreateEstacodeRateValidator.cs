using EmployeeServiceApi.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class CreateEstacodeRateValidator
    : AbstractValidator<CreateEstacodeRateDto>
{
    public CreateEstacodeRateValidator()
    {
        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$")
            .WithMessage(
                "Country code must be a two-letter ISO country code.");

        RuleFor(x => x.Grade)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.DailyRateUsd)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false);
    }
}