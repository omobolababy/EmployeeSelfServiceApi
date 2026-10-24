using EmployeeServiceApi.Entities;
using EmployeeServiceApi.Entities.KPI;
using EmployeeServiceApi.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Data;

/// <summary>
/// Applies EF Core migrations at startup and (when Seed:Enabled is true)
/// loads sample accounts and reference data once, on an empty database.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        await context.Database.MigrateAsync();

        if (!configuration.GetValue("Seed:Enabled", false))
        {
            return;
        }

        if (await context.Employees.AnyAsync())
        {
            return;
        }

        var password = configuration["Seed:DefaultPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Password123!";
            logger.LogWarning(
                "Seed:DefaultPassword is not set; sample accounts use the development password '{Password}'.",
                password);
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var now = DateTime.UtcNow;

        var roles = await context.Roles.ToDictionaryAsync(r => r.Name);

        Employee Make(
            string number, string first, string last, string email,
            string department, string grade, params string[] roleNames)
        {
            var employee = new Employee
            {
                EmployeeNumber = number,
                FirstName = first,
                LastName = last,
                Email = email.ToLowerInvariant(),
                Department = department,
                Grade = grade,
                PhoneNumber = null,
                PasswordHash = hash,
                Status = EmployeeStatus.Active,
                CreatedAtUtc = now,
                ActivatedAtUtc = now,
                PasswordChangedAtUtc = now
            };

            // Every active employee is implicitly Staff.
            foreach (var roleName in roleNames.Append("Staff").Distinct())
            {
                employee.EmployeeRoles.Add(new EmployeeRoles { Role = roles[roleName] });
            }

            return employee;
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@example.com";

        var admin = Make("EMP001", "Barakah", "Admin", "ibarakah3@gmail.com", "Administration", "G5", "Admin");
        var manager = Make("EMP002", "Michael", "Manager", "barakahisiaq861@gmail.com", "Engineering", "G4", "Manager");
        var hr1 = Make("EMP003", "Hannah", "HR", "amkolawole1@gmail.com", "Human Resources", "G4", "HR");
        var hr2 = Make("EMP004", "Hakeem", "HR", "hr2@example.com", "Human Resources", "G4", "HR");
        var finance1 = Make("EMP005", "Funmi", "Finance", "finance1@example.com", "Finance", "G4", "Finance");
        var finance2 = Make("EMP006", "Femi", "Finance", "finance2@example.com", "Finance", "G4", "Finance");
        var staff1 = Make("EMP007", "Sade", "Staff", "staff1@example.com", "Engineering", "G2");
        var staff2 = Make("EMP008", "Segun", "Staff", "staff2@example.com", "Engineering", "G3");

        context.Employees.AddRange(admin, manager, hr1, hr2, finance1, finance2, staff1, staff2);
        await context.SaveChangesAsync();

        // Reporting lines (need generated ids, so a second save).
        staff1.ReportingManagerId = manager.Id;
        staff2.ReportingManagerId = manager.Id;
        manager.ReportingManagerId = admin.Id;
        hr1.ReportingManagerId = admin.Id;
        hr2.ReportingManagerId = admin.Id;
        finance1.ReportingManagerId = admin.Id;
        finance2.ReportingManagerId = admin.Id;

        // Estacode daily rates (USD) by destination country and grade.
        var rates = new (string Country, string Grade, decimal Rate)[]
        {
            ("GB", "G1", 150m), ("GB", "G2", 180m), ("GB", "G3", 220m), ("GB", "G4", 260m), ("GB", "G5", 300m),
            ("US", "G1", 160m), ("US", "G2", 190m), ("US", "G3", 230m), ("US", "G4", 270m), ("US", "G5", 310m),
            ("AE", "G1", 140m), ("AE", "G2", 170m), ("AE", "G3", 200m), ("AE", "G4", 240m), ("AE", "G5", 280m),
            ("ZA", "G1", 120m), ("ZA", "G2", 150m), ("ZA", "G3", 180m), ("ZA", "G4", 210m), ("ZA", "G5", 250m)
        };

        context.EstacodeRates.AddRange(rates.Select(r => new EstaCodeRate
        {
            CountryCode = r.Country,
            Grade = r.Grade,
            DailyRateUsd = r.Rate,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }));

        // One open KPI review period.
        var year = DateTime.UtcNow.Year;

        context.KpiReviewPeriods.Add(new KpiReviewPeriod
        {
            Name = $"{year} Annual Review",
            StartDate = new DateOnly(year, 1, 1),
            EndDate = new DateOnly(year, 12, 31),
            SubmissionDeadline = new DateOnly(year, 12, 31),
            IsOpen = true,
            CreatedAtUtc = now
        });

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded sample data. Sign in with {Email} (and hr1/hr2/finance1/finance2/manager/staff1/staff2@example.com).",
            adminEmail);
    }
}
