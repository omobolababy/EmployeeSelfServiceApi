using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmployeeSelfServiceApi.Data;
using EmployeeSelfServiceApi.DTOs.Common;
using EmployeeSelfServiceApi.Filters;
using EmployeeSelfServiceApi.Middleware;
using EmployeeSelfServiceApi.Repositories.Implementation;
using EmployeeSelfServiceApi.Repositories.Interfaces;
using EmployeeSelfServiceApi.Services;
using EmployeeSelfServiceApi.Services.Implementation;
using EmployeeSelfServiceApi.Services.Interfaces;
using EmployeeServiceApi.Configuration;
using EmployeeServiceApi.DTOs.Common;
using EmployeeServiceApi.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

// QuestPDF requires a license type to be declared before any PDF is built.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// JWT configuration (key comes from user secrets / environment variables)
// ---------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing. Run: dotnet user-secrets set \"Jwt:Key\" \"<random string of 32+ characters>\"");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

// ---------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------
builder.Services.Configure<SmtpMail>(
    builder.Configuration.GetSection("SmtpMail"));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<ILmsService, LmsService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IPayslipPdfService, PayslipPdfService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Background workers: queued email + daily reminders
builder.Services.AddHostedService<EmailQueueBackgroundService>();
builder.Services.AddHostedService<ReminderBackgroundService>();

// FluentValidation validators (picked up by FluentValidationFilter)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---------------------------------------------------------------------
// Authentication / authorization
// ---------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            // Account status is checked on EVERY request, so a token issued
            // before deactivation is rejected with 401.
            OnTokenValidated = async context =>
            {
                var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(idValue, out var employeeId))
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

                var isActive = await db.Employees
                    .AsNoTracking()
                    .AnyAsync(
                        e => e.Id == employeeId &&
                             e.Status == EmployeeStatus.Active,
                        context.HttpContext.RequestAborted);

                if (!isActive)
                {
                    context.Fail("The account is not active.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------
// MVC, JSON, validation, Swagger
// ---------------------------------------------------------------------
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<FluentValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model-binding errors use the same error wrapper as everything else.
        options.InvalidModelStateResponseFactory = actionContext =>
        {
            var errors = actionContext.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiErrorDto
                {
                    Field = entry.Key,
                    Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The value is invalid."
                        : error.ErrorMessage
                }))
                .ToList();

            return new BadRequestObjectResult(new ApiErrorResponseDto
            {
                ResponseCode = StatusCodes.Status400BadRequest,
                ResponseMessage = "Validation failed.",
                Errors = errors
            });
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Self-Service API",
        Version = "v1"
    });

    // Several DTOs share short names across namespaces.
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste the JWT only (Swagger adds the \"Bearer \" prefix).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// 401 / 403 produced by the auth pipeline use the same JSON wrapper.
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;

    if (response.HasStarted ||
        response.ContentLength.HasValue ||
        !string.IsNullOrEmpty(response.ContentType))
    {
        return;
    }

    var message = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => "Authentication is required or the token is invalid.",
        StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        _ => "The request could not be completed."
    };

    response.ContentType = "application/json";

    await response.WriteAsync(JsonSerializer.Serialize(
        new ApiErrorResponseDto
        {
            ResponseCode = response.StatusCode,
            ResponseMessage = message
        },
        new JsonSerializerOptions(JsonSerializerDefaults.Web)));
});

// Create / update the database and seed sample data at startup.
await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
