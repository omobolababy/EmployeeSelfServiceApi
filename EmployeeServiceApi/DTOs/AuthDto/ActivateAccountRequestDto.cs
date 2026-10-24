namespace EmployeeServiceApi.DTOs;

public class ActivateAccountRequestDto
{
    public string Token { get; set; } = string.Empty;
    
    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;
}