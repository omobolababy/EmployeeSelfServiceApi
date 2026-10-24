namespace EmployeeServiceApi.DTOs.TrainingDto;

public class ReassignTrainingReviewerDto
{
    public int NewReviewerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}