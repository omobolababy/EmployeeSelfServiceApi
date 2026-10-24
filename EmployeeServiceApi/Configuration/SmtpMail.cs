namespace EmployeeServiceApi.Configuration;

public class SmtpMail
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Employee Self-Service";
    public string SenderEmail { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true; 
}