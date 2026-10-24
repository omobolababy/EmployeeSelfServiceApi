using EmployeeServiceApi.DTOs;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Auth;

public class ForgotPasswordRequestValidator
    : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Enter a valid email address.");
    }
}