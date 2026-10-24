using EmployeeSelfServiceApi.Data;
using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.KPI;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmployeeServiceApi.Tests;

/// <summary>
/// Fresh in-memory database per test. The seeded roles (HasData) are present.
/// </summary>
public static class TestDb
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<Employee> AddEmployeeAsync(
        ApplicationDbContext context,
        string firstName,
        string department = "Engineering",
        string grade = "G2",
        int? managerId = null,
        params string[] roles)
    {
        var employee = new Employee
        {
            EmployeeNumber = $"EMP-{Guid.NewGuid():N}"[..12],
            FirstName = firstName,
            LastName = "Tester",
            Email = $"{firstName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
            Department = department,
            Grade = grade,
            PasswordHash = "hash",
            Status = EmployeeStatus.Active,
            ReportingManagerId = managerId,
            CreatedAtUtc = DateTime.UtcNow,
            ActivatedAtUtc = DateTime.UtcNow
        };

        foreach (var roleName in roles.Append("Staff").Distinct())
        {
            var role = await context.Roles.FirstAsync(r => r.Name == roleName);
            employee.EmployeeRoles.Add(new EmployeeRoles { Role = role });
        }

        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }

    public static async Task<KpiReviewPeriod> AddOpenPeriodAsync(ApplicationDbContext context)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var period = new KpiReviewPeriod
        {
            Name = $"Period {Guid.NewGuid():N}",
            StartDate = today.AddDays(-30),
            EndDate = today.AddDays(30),
            SubmissionDeadline = today.AddDays(30),
            IsOpen = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.KpiReviewPeriods.Add(period);
        await context.SaveChangesAsync();
        return period;
    }
}
