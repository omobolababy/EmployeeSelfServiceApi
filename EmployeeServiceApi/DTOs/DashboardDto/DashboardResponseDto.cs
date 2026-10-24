namespace EmployeeSelfServiceApi.DTOs.Dashboard;

public class DashboardResponseDto
{
    public int PendingTrainingRequests { get; set; }

    public int IncompleteCourses { get; set; }

    public int OverdueCourses { get; set; }

    public int KpiPlansAwaitingMyAction { get; set; }

    public int KpiReviewsAwaitingMyAction { get; set; }

    public int TrainingReviewsAwaitingMyAction { get; set; }

    public int ActiveApplications { get; set; }

    public int UnreadNotifications { get; set; }

    public int PublishedPayslipPeriods { get; set; }

    public DateTime GeneratedAtUtc { get; set; }
}