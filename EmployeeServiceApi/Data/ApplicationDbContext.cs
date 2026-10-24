
using EmployeeSelfServiceApi.Entities;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.JobApplication;
using EmployeeServiceApi.Entities.KPI;
using EmployeeServiceApi.Entities.LMS;
using EmployeeServiceApi.Entities.Payslip;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Employees and access control
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EmployeeRoles> EmployeeRoles => Set<EmployeeRoles>();
    public DbSet<EmployeeSetupToken> EmployeeSetupTokens => Set<EmployeeSetupToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Notifications, email and audit
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<EmailQueueItem> EmailQueueItems => Set<EmailQueueItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Training and allowances
    public DbSet<TrainingRequest> TrainingRequests => Set<TrainingRequest>();
    public DbSet<TrainingApprovalHistory> TrainingApprovalHistories => Set<TrainingApprovalHistory>();
    public DbSet<EstaCodeRate> EstacodeRates => Set<EstaCodeRate>();
    public DbSet<TrainingAllowanceVersion> TrainingAllowanceVersions => Set<TrainingAllowanceVersion>();
    public DbSet<TrainingCompletionEvidence> TrainingCompletionEvidence =>
        Set<TrainingCompletionEvidence>();
    
    // Learning management
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseLesson> CourseLessons => Set<CourseLesson>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<AssessmentOption> AssessmentOptions => Set<AssessmentOption>();
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();
    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();
    public DbSet<AssignmentResetHistory> AssignmentResetHistories => Set<AssignmentResetHistory>();

    // KPI and performance
    public DbSet<KpiReviewPeriod> KpiReviewPeriods => Set<KpiReviewPeriod>();
    public DbSet<KpiPlan> KpiPlans => Set<KpiPlan>();
    public DbSet<KpiTarget> KpiTargets => Set<KpiTarget>();
    public DbSet<KpiReviewHistory> KpiReviewHistories => Set<KpiReviewHistory>();
    public DbSet<KpiReopeningHistory> KpiReopeningHistories => Set<KpiReopeningHistory>();

    // Payroll
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<PayslipVersion> PayslipVersions => Set<PayslipVersion>();
    public DbSet<PayslipItem> PayslipItems => Set<PayslipItem>();

    // Opportunities
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityDepartmentEligibility> OpportunityDepartmentEligibilities => Set<OpportunityDepartmentEligibility>();
    public DbSet<OpportunityGradeEligibility> OpportunityGradeEligibilities => Set<OpportunityGradeEligibility>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<JobApplicationHistory> JobApplicationHistories => Set<JobApplicationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEmployees(modelBuilder);
        ConfigureAuthentication(modelBuilder);
        ConfigureNotifications(modelBuilder);
        ConfigureTraining(modelBuilder);
        ConfigureCourses(modelBuilder);
        ConfigureKpis(modelBuilder);
        ConfigurePayroll(modelBuilder);
        ConfigureOpportunities(modelBuilder);
    }

    private static void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EmployeeNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.Department)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Grade)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.ReportingManager)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(r => r.Name).IsUnique();

            entity.HasData(
                new Role { Id = 1, Name = "Staff" },
                new Role { Id = 2, Name = "Manager" },
                new Role { Id = 3, Name = "HR" },
                new Role { Id = 4, Name = "Finance" },
                new Role { Id = 5, Name = "Admin" }
            );
        });

        modelBuilder.Entity<EmployeeRoles>(entity =>
        {
            entity.HasKey(er => new { er.EmployeeId, er.RoleId });

            entity.HasOne(er => er.Employee)
                .WithMany(e => e.EmployeeRoles)
                .HasForeignKey(er => er.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(er => er.Role)
                .WithMany(r => r.EmployeeRoles)
                .HasForeignKey(er => er.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuthentication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeSetupToken>(entity =>
        {
            entity.Property(t => t.TokenHash)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(t => t.TokenHash).IsUnique();

            entity.HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(t => t.TokenHash)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(t => t.TokenHash).IsUnique();

            entity.HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(n => n.Message)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(n => n.RelatedEntityType)
                .HasMaxLength(100);

            entity.Property(n => n.DeduplicationKey)
                .HasMaxLength(200);

            entity.HasIndex(n => new { n.EmployeeId, n.CreatedAtUtc });

            entity.HasIndex(n => new { n.EmployeeId, n.DeduplicationKey })
                .IsUnique()
                .HasFilter("[DeduplicationKey] IS NOT NULL");

            entity.HasOne(n => n.Employee)
                .WithMany(e => e.Notifications)
                .HasForeignKey(n => n.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailQueueItem>(entity =>
        {
            entity.Property(e => e.RecipientEmail)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.Subject)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(e => e.LockId)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Action)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(a => a.EntityType)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(a => a.EntityId)
                .HasMaxLength(100);

            entity.Property(a => a.Reason)
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Category).HasMaxLength(100).IsRequired();
            entity.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(a => a.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(a => a.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(a => a.ContentType).HasMaxLength(150).IsRequired();

            entity.HasIndex(a => new { a.EntityType, a.EntityId });
        });
    }

    private static void ConfigureTraining(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrainingRequest>(entity =>
        {
            entity.Property(t => t.RequestNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(t => t.RequestNumber).IsUnique();

            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Provider).HasMaxLength(200).IsRequired();
            entity.Property(t => t.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(t => t.City).HasMaxLength(150).IsRequired();
            entity.Property(t => t.CourseFeeCurrency).HasMaxLength(3).IsRequired();
            entity.Property(t => t.FlightCurrency).HasMaxLength(3).IsRequired();

            entity.Property(t => t.CourseFee).HasPrecision(18, 2);
            entity.Property(t => t.FlightEstimate).HasPrecision(18, 2);

            entity.HasOne(t => t.Employee)
                .WithMany(e => e.TrainingRequests)
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedReviewer)
                .WithMany()
                .HasForeignKey(t => t.AssignedReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainingApprovalHistory>(entity =>
        {
            entity.Property(h => h.ActorRole).HasMaxLength(50).IsRequired();
            entity.Property(h => h.Comment).HasMaxLength(2000);
            entity.Property(h => h.Reason).HasMaxLength(1000);

            entity.HasOne(h => h.TrainingRequest)
                .WithMany(t => t.ApprovalHistory)
                .HasForeignKey(h => h.TrainingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ActorEmployee)
                .WithMany()
                .HasForeignKey(h => h.ActorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EstaCodeRate>(entity =>
        {
            entity.Property(r => r.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(r => r.Grade).HasMaxLength(50).IsRequired();
            entity.Property(r => r.DailyRateUsd).HasPrecision(18, 2);

            entity.HasIndex(r => new { r.CountryCode, r.Grade })
                .IsUnique();
        });

        modelBuilder.Entity<TrainingAllowanceVersion>(entity =>
        {
            entity.Property(a => a.CountryCodeSnapshot).HasMaxLength(2).IsRequired();
            entity.Property(a => a.GradeSnapshot).HasMaxLength(50).IsRequired();
            entity.Property(a => a.DailyRateUsdSnapshot).HasPrecision(18, 2);
            entity.Property(a => a.TotalAmountUsd).HasPrecision(18, 2);
            entity.Property(a => a.CorrectionReason).HasMaxLength(1000);

            entity.HasIndex(a => new { a.TrainingRequestId, a.VersionNumber })
                .IsUnique();

            entity.HasOne(a => a.TrainingRequest)
                .WithMany(t => t.AllowanceVersions)
                .HasForeignKey(a => a.TrainingRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    

    private static void ConfigureCourses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Category).HasMaxLength(100);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(c => c.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseLesson>(entity =>
        {
            entity.Property(l => l.Title).HasMaxLength(200).IsRequired();
            entity.Property(l => l.VideoUrl).HasMaxLength(1000);

            entity.HasIndex(l => new { l.CourseId, l.OrderNumber })
                .IsUnique();

            entity.HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentQuestion>(entity =>
        {
            entity.Property(q => q.QuestionText).HasMaxLength(2000).IsRequired();
            entity.Property(q => q.Marks).HasPrecision(10, 2);

            entity.HasIndex(q => new { q.CourseId, q.OrderNumber })
                .IsUnique();

            entity.HasOne(q => q.Course)
                .WithMany(c => c.Questions)
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentOption>(entity =>
        {
            entity.Property(o => o.OptionText).HasMaxLength(1000).IsRequired();

            entity.HasOne(o => o.AssessmentQuestion)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.AssessmentQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CourseAssignment>(entity =>
        {
            entity.HasIndex(a => new { a.EmployeeId, a.CourseId })
                .IsUnique();

            entity.Property(a => a.BestScorePercentage).HasPrecision(5, 2);

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.CourseAssignments)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Course)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonCompletion>(entity =>
        {
            entity.HasIndex(l => new { l.CourseAssignmentId, l.CourseLessonId })
                .IsUnique();

            entity.HasOne(l => l.CourseAssignment)
                .WithMany(a => a.LessonCompletions)
                .HasForeignKey(l => l.CourseAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<CourseLesson>()
                .WithMany()
                .HasForeignKey(l => l.CourseLessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssessmentAttempt>(entity =>
        {
            entity.Property(a => a.Score).HasPrecision(10, 2);
            entity.Property(a => a.ScorePercentage).HasPrecision(5, 2);

            entity.HasIndex(a => new
                {
                    a.CourseAssignmentId,
                    a.AttemptCycle,
                    a.AttemptNumber
                })
                .IsUnique();

            entity.HasOne(a => a.CourseAssignment)
                .WithMany(c => c.AssessmentAttempts)
                .HasForeignKey(a => a.CourseAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentAnswer>(entity =>
        {
            entity.Property(a => a.MarksAwarded).HasPrecision(10, 2);

            entity.HasOne(a => a.AssessmentAttempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(a => a.AssessmentAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AssessmentQuestion>()
                .WithMany()
                .HasForeignKey(a => a.AssessmentQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<AssessmentOption>()
                .WithMany()
                .HasForeignKey(a => a.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssignmentResetHistory>(entity =>
        {
            entity.Property(r => r.Reason).HasMaxLength(1000).IsRequired();

            entity.HasOne(r => r.CourseAssignment)
                .WithMany(a => a.ResetHistory)
                .HasForeignKey(r => r.CourseAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(r => r.ResetByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureKpis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiReviewPeriod>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<KpiPlan>(entity =>
        {
            entity.Property(p => p.FinalScorePercentage).HasPrecision(5, 2);
            entity.Property(p => p.ReviewerComment).HasMaxLength(2000);

            entity.HasIndex(p => new { p.EmployeeId, p.ReviewPeriodId })
                .IsUnique();

            entity.HasOne(p => p.Employee)
                .WithMany(e => e.KpiPlans)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.AssignedReviewer)
                .WithMany()
                .HasForeignKey(p => p.AssignedReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.ReviewPeriod)
                .WithMany(r => r.Plans)
                .HasForeignKey(p => p.ReviewPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KpiTarget>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Unit).HasMaxLength(50).IsRequired();
            entity.Property(t => t.TargetValue).HasPrecision(18, 2);
            entity.Property(t => t.WeightPercentage).HasPrecision(5, 2);
            entity.Property(t => t.ActualValue).HasPrecision(18, 2);
            entity.Property(t => t.ScorePercentage).HasPrecision(5, 2);

            entity.HasOne(t => t.KpiPlan)
                .WithMany(p => p.Targets)
                .HasForeignKey(t => t.KpiPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KpiReviewHistory>(entity =>
        {
            entity.Property(h => h.Action).HasMaxLength(100).IsRequired();
            entity.Property(h => h.PreviousStatus).HasMaxLength(50);
            entity.Property(h => h.NewStatus).HasMaxLength(50);
            entity.Property(h => h.Comment).HasMaxLength(2000);

            entity.HasOne(h => h.KpiPlan)
                .WithMany(p => p.ReviewHistory)
                .HasForeignKey(h => h.KpiPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KpiReopeningHistory>(entity =>
        {
            entity.Property(h => h.Reason).HasMaxLength(1000).IsRequired();

            entity.HasOne(h => h.KpiPlan)
                .WithMany(p => p.ReopeningHistory)
                .HasForeignKey(h => h.KpiPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(h => h.ReopenedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayroll(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasIndex(p => new
                {
                    p.EmployeeId,
                    p.PayrollYear,
                    p.PayrollMonth
                })
                .IsUnique();

            entity.HasOne(p => p.Employee)
                .WithMany(e => e.Payslips)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayslipVersion>(entity =>
        {
            entity.Property(v => v.BasicSalary).HasPrecision(18, 2);
            entity.Property(v => v.GrossPay).HasPrecision(18, 2);
            entity.Property(v => v.TotalDeductions).HasPrecision(18, 2);
            entity.Property(v => v.NetPay).HasPrecision(18, 2);
            entity.Property(v => v.Currency).HasMaxLength(3).IsRequired();
            entity.Property(v => v.RevisionReason).HasMaxLength(1000);

            entity.HasIndex(v => new { v.PayslipId, v.VersionNumber })
                .IsUnique();

            entity.HasOne(v => v.Payslip)
                .WithMany(p => p.Versions)
                .HasForeignKey(v => v.PayslipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(v => v.PreparedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(v => v.PublishedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayslipItem>(entity =>
        {
            entity.Property(i => i.Name).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Amount).HasPrecision(18, 2);

            entity.HasOne(i => i.PayslipVersion)
                .WithMany(v => v.Items)
                .HasForeignKey(i => i.PayslipVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure the optional current-version reference separately from
        // the collection of all versions.
        modelBuilder.Entity<Payslip>()
            .HasOne<PayslipVersion>()
            .WithMany()
            .HasForeignKey(p => p.CurrentVersionId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<Payslip>()
            .Property(p => p.RowVersion)
            .IsRowVersion();
    }

    private static void ConfigureOpportunities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.Property(o => o.Title).HasMaxLength(200).IsRequired();

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(o => o.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OpportunityDepartmentEligibility>(entity =>
        {
            entity.Property(e => e.Department).HasMaxLength(150).IsRequired();

            entity.HasIndex(e => new { e.OpportunityId, e.Department })
                .IsUnique();

            entity.HasOne(e => e.Opportunity)
                .WithMany(o => o.EligibleDepartments)
                .HasForeignKey(e => e.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OpportunityGradeEligibility>(entity =>
        {
            entity.Property(e => e.Grade).HasMaxLength(50).IsRequired();

            entity.HasIndex(e => new { e.OpportunityId, e.Grade })
                .IsUnique();

            entity.HasOne(e => e.Opportunity)
                .WithMany(o => o.EligibleGrades)
                .HasForeignKey(e => e.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.Property(a => a.DepartmentSnapshot).HasMaxLength(150).IsRequired();
            entity.Property(a => a.GradeSnapshot).HasMaxLength(50).IsRequired();
            entity.Property(a => a.CoverLetter).HasMaxLength(5000);

            entity.HasIndex(a => new { a.EmployeeId, a.OpportunityId })
                .IsUnique();

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.JobApplications)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Opportunity)
                .WithMany(o => o.Applications)
                .HasForeignKey(a => a.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobApplicationHistory>(entity =>
        {
            entity.Property(h => h.Comment).HasMaxLength(2000);

            entity.HasOne(h => h.JobApplication)
                .WithMany(a => a.History)
                .HasForeignKey(h => h.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(h => h.ActorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<TrainingCompletionEvidence>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.ReviewComment)
                .HasMaxLength(1000);

            entity.HasOne(e => e.TrainingRequest)
                .WithMany(r => r.CompletionEvidence)
                .HasForeignKey(e => e.TrainingRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Attachment)
                .WithMany()
                .HasForeignKey(e => e.AttachmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new
            {
                e.TrainingRequestId,
                e.SubmittedAtUtc
            });
        });
    }
}
