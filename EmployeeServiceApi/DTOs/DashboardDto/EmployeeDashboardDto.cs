namespace EmployeeSelfServiceApi.DTOs.Dashboard;

public class EmployeeDashboardDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;

    public int UnreadNotifications { get; set; }
    public int PendingTrainingRequests { get; set; }
    public int AssignedCourses { get; set; }
    public int IncompleteCourses { get; set; }
    public int PendingKpiPlans { get; set; }
    public int PublishedPayslips { get; set; }
    public int OpenOpportunities { get; set; }
    public int SubmittedApplications { get; set; }
}