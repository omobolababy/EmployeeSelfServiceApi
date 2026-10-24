using System.Security.Claims;
using EmployeeSelfServiceApi.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeSelfServiceApi.Controllers;


/// Controllers handle HTTP only. Business rules live in the services, and
/// errors are turned into the standard wrapper by ExceptionHandlingMiddleware.

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentEmployeeId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out var id) || id <= 0)
            {
                throw new ApiException(
                    StatusCodes.Status401Unauthorized,
                    "The token does not contain a valid employee id.");
            }

            return id;
        }
    }
}
