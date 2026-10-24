namespace EmployeeSelfServiceApi.DTOs.Dashboard;

public class AdminDashboardDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int PendingActivations { get; set; }

    public int PendingEmailMessages { get; set; }
    public int FailedEmailMessages { get; set; }

    public int TotalTrainingRequests { get; set; }
    public int TotalCourseAssignments { get; set; }
    public int TotalKpiPlans { get; set; }
    public int TotalPayslips { get; set; }
    public int TotalApplications { get; set; }
}