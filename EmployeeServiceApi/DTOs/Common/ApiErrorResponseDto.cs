using EmployeeServiceApi.DTOs.Common;

namespace EmployeeSelfServiceApi.DTOs.Common;

public class ApiErrorResponseDto
{
    public int ResponseCode { get; set; }
    public string ResponseMessage { get; set; } = string.Empty;
    public List<ApiErrorDto> Errors { get; set; } = new();
}