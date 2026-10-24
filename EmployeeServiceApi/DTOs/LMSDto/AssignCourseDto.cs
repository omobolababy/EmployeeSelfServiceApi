namespace EmployeeSelfServiceApi.DTOs.LMS;

public class AssignCourseDto
{
    public int CourseId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime? DueAtUtc { get; set; }
}