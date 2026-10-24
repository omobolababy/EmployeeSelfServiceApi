using EmployeeSelfServiceApi.Entities;
using EmployeeServiceApi.Entities.KPI;
using EmployeeServiceApi.Enums;

namespace EmployeeServiceApi.Entities;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public string Grade { get; set; }
    public string? PhoneNumber { get; set; }
    public int? ReportingManagerId { get; set; }
    public Employee? ReportingManager { get; set; } 
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public ICollection<EmployeeRoles> EmployeeRoles { get; set; } = new List<EmployeeRoles>();
    public string PasswordHash { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.PendingActivation;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ActivateAtUtc { get; set; }
    
    public int FailedLoginAttempts { get; set; }
    public DateTime? FirstFailedLoginAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? PasswordChangedAtUtc { get; set; }
    public DateTime ActivatedAtUtc { get; set; }
    public ICollection<TrainingRequest> TrainingRequests { get; set; } = new List<TrainingRequest>();

    public ICollection<CourseAssignment> CourseAssignments { get; set; }
        = new List<CourseAssignment>();

    public ICollection<KpiPlan> KpiPlans { get; set; }
        = new List<KpiPlan>();

    public ICollection<Payslip.Payslip> Payslips { get; set; }
        = new List<Payslip.Payslip>();

    public ICollection<JobApplication.JobApplication> JobApplications { get; set; }
        = new List<JobApplication.JobApplication>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
    
}