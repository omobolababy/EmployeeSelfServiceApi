using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeSelfServiceApi.Filters;

/// <summary>
/// Runs the FluentValidation validator (if one is registered) for every
/// action argument. A failure throws ValidationException, which the
/// exception middleware turns into a 400 with field-level errors.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var argumentType = argument.GetType();

            if (argumentType.IsPrimitive || argumentType == typeof(string))
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = ValidationContext<object>.CreateWithOptions(
                argument,
                options => { });

            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
