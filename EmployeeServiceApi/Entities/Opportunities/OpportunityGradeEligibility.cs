namespace EmployeeServiceApi.Entities.JobApplication;

public class OpportunityGradeEligibility
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public Opportunity Opportunity { get; set; } = null!;

    public string Grade { get; set; } = string.Empty;
}