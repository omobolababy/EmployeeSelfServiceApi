using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Opportunities;

public class UpdateApplicationStatusDto
{
    public ApplicationStatus Status { get; set; }
    public string? Comment { get; set; }
}