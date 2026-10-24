namespace EmployeeSelfServiceApi.DTOs.Dashboard;

public class ManagerDashboardDto
{
    public int TeamSize { get; set; }
    public int PendingTrainingApprovals { get; set; }
    public int PendingKpiReviews { get; set; }
    public int TeamCoursesIncomplete { get; set; }
    public int UnreadNotifications { get; set; }
}