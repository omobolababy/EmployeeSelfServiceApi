using EmployeeServiceApi.Enums;

namespace EmployeeSelfServiceApi.DTOs.Training;

public class TrainingRequestResponseDto
{
    public int Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public DateOnly TrainingStartDate { get; set; }

    public DateOnly TrainingEndDate { get; set; }

    public DateOnly? DepartureDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public string BusinessJustification { get; set; } = string.Empty;

    public decimal CourseFee { get; set; }

    public string CourseFeeCurrency { get; set; } = string.Empty;

    public decimal? FlightEstimate { get; set; }

    public string? FlightCurrency { get; set; }

    public TrainingStatus Status { get; set; }

    public int? AssignedReviewerId { get; set; }

    public string? AssignedReviewerName { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? WithdrawnAtUtc { get; set; }
}