using EmployeeServiceApi.DTOs.Training;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Training;

public class UpdateEstacodeRateValidator
    : AbstractValidator<UpdateEstacodeRateDto>
{
    public UpdateEstacodeRateValidator()
    {
        RuleFor(x => x.DailyRateUsd)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, false);
    }
}