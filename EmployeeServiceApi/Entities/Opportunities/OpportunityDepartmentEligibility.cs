namespace EmployeeServiceApi.Entities.JobApplication;

public class OpportunityDepartmentEligibility
{
    public int Id { get; set; }

    public int OpportunityId { get; set; }

    public Opportunity Opportunity { get; set; } = null!;

    public string Department { get; set; } = string.Empty;
}