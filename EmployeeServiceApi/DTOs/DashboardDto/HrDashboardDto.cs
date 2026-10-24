namespace EmployeeSelfServiceApi.DTOs.Dashboard;

public class HrDashboardDto
{
    public int ActiveEmployees { get; set; }
    public int PendingActivations { get; set; }
    public int PendingTrainingApprovals { get; set; }
    public int PublishedCourses { get; set; }
    public int OpenOpportunities { get; set; }
    public int PendingApplications { get; set; }
}