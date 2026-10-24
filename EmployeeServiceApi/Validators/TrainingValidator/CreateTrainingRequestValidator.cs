
using EmployeeSelfServiceApi.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class CreateTrainingRequestValidator
    : AbstractValidator<CreateTrainingRequestDto>
{
    public CreateTrainingRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Country code must be a two-letter ISO country code.");

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TrainingStartDate)
            .NotEmpty();

        RuleFor(x => x.TrainingEndDate)
            .GreaterThanOrEqualTo(x => x.TrainingStartDate)
            .WithMessage("Training end date cannot be before the start date.");

        RuleFor(x => x.DepartureDate)
            .LessThanOrEqualTo(x => x.TrainingStartDate)
            .When(x => x.DepartureDate.HasValue)
            .WithMessage("Departure date cannot be after training starts.");

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.TrainingEndDate)
            .When(x => x.ReturnDate.HasValue)
            .WithMessage("Return date cannot be before training ends.");

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.DepartureDate!.Value)
            .When(x => x.DepartureDate.HasValue && x.ReturnDate.HasValue)
            .WithMessage("Return date cannot be before departure date.");

        RuleFor(x => x.BusinessJustification)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.CourseFee)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.CourseFeeCurrency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Course fee currency must be a three-letter currency code.");

        RuleFor(x => x.FlightEstimate)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false)
            .When(x => x.FlightEstimate.HasValue);

        RuleFor(x => x.FlightCurrency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .When(x => x.FlightEstimate.HasValue)
            .WithMessage("Flight currency must be a three-letter currency code.");
    }
}
