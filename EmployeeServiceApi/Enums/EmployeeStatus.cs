namespace EmployeeServiceApi.Enums;

public enum EmployeeStatus
{
    PendingActivation = 1,
    Active = 2,
    Inactive = 3
}

public enum TrainingStatus
{
    Draft = 1,
    PendingManagerApproval = 2,
    PendingHRApproval = 3,
    PendingFinanceApproval = 4,
    ReturnedToHR = 5,
    Approved = 6,
    Completed = 7,
    Rejected = 8,
    Withdrawn = 9,
    RejectedNoEvidence = 10
}

public enum TrainingDecision
{
    Approve = 1,
    Reject = 2,
    ReturnToHR = 3,
    ReturnToEmployee = 4
}

public enum AllowanceStatus
{
    Provisional = 1,
    Approved = 2,
    Superseded = 3
}

public enum CourseStatus
{
    Draft = 1,
    Published = 2
}

public enum CourseAssignmentStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    AttemptsExhausted = 4
}

public enum KpiPlanStatus
{
    Draft = 1,
    Active = 2,
    Submitted = 3,
    Returned = 4,
    Finalized = 5,
    Reviewed = 6
}

public enum KpiReviewDecision
{
    Approve = 1,
    ReturnToEmployee = 2
}

public enum PayslipStatus
{
    Draft = 1,
    Published = 2,
    Superseded = 3
}

public enum OpportunityType
{
    InternalVacancy = 1,
    DevelopmentOpportunity = 2
}

public enum OpportunityStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3
}

public enum ApplicationStatus
{
    Submitted = 1,
    Shortlisted = 2,
    Selected = 3,
    Rejected = 4,
    Withdrawn = 5
}

public enum EmailMessageStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4
}

public enum NotificationType
{
    AccountSetup = 1,
    PasswordReset = 2,
    Training = 3,
    CourseAssignment = 4,
    Kpi = 5,
    Payslip = 6,
    Application = 7,
    Reminder = 8,
    General = 9,
    Published = 10,
}

public enum PayslipItemType
{
    Allowance = 1,
    Deduction = 2
}

public enum TrainingEvidenceStatus
{
    PendingReview = 1,
    Accepted = 2,
    Returned = 3
}