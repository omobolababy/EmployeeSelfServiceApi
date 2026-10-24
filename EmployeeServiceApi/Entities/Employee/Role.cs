namespace EmployeeServiceApi.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public ICollection<EmployeeRoles> EmployeeRoles { get; set; } = new List<EmployeeRoles>();
}