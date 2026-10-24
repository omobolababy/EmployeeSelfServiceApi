using EmployeeSelfServiceApi.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class UpdateTrainingRequestValidator
    : AbstractValidator<UpdateTrainingRequestDto>
{
    public UpdateTrainingRequestValidator()
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
            .Matches("^[A-Za-z]{2}$");

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TrainingEndDate)
            .GreaterThanOrEqualTo(x => x.TrainingStartDate);

        RuleFor(x => x.DepartureDate)
            .LessThanOrEqualTo(x => x.TrainingStartDate)
            .When(x => x.DepartureDate.HasValue);

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.TrainingEndDate)
            .When(x => x.ReturnDate.HasValue);

        RuleFor(x => x.ReturnDate)
            .GreaterThanOrEqualTo(x => x.DepartureDate!.Value)
            .When(x => x.DepartureDate.HasValue && x.ReturnDate.HasValue);

        RuleFor(x => x.BusinessJustification)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.CourseFee)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false);

        RuleFor(x => x.CourseFeeCurrency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");

        RuleFor(x => x.FlightEstimate)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false)
            .When(x => x.FlightEstimate.HasValue);

        RuleFor(x => x.FlightCurrency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .When(x => x.FlightEstimate.HasValue);
    }
}