using EmployeeSelfServiceApi.DTOs.Common;
using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public List<ApiErrorDto> Errors { get; }

    public ApiException(
        int statusCode,
        string message,
        List<ApiErrorDto>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new List<ApiErrorDto>();
    }
}