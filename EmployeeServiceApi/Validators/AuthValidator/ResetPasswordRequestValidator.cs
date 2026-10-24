using EmployeeServiceApi.DTOs;
using FluentValidation;

namespace EmployeeSelfServiceApi.Validators.Auth;

public class ResetPasswordRequestValidator
    : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("The password reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(10)
            .WithMessage("Password must contain at least 10 characters.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain a digit.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}