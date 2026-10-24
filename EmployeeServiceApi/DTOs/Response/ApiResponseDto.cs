
using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.DTOs.Common;

public class ApiResponseDto<T>
{
    public int ResponseCode { get; set; }

    public string ResponseMessage { get; set; } = string.Empty;

    public T? Data { get; set; }

    public List<ApiErrorDto> Errors { get; set; } = new();
}
