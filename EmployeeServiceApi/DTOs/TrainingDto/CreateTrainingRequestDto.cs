using EmployeeServiceApi.Entities.LMS;

namespace EmployeeSelfServiceApi.DTOs.Training;

public class CreateTrainingRequestDto
{
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

    public string CourseFeeCurrency { get; set; } = "NGN";

    public decimal? FlightEstimate { get; set; }

    public string? FlightCurrency { get; set; }
}