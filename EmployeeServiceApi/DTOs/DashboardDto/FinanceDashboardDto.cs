namespace EmployeeSelfServiceApi.DTOs.Dashboard;

public class FinanceDashboardDto
{
    public int PendingTrainingApprovals { get; set; }
    public int DraftPayslips { get; set; }
    public int PublishedPayslips { get; set; }
    public int FailedEmails { get; set; }
}