using System.Text.Json;
using System.Text.RegularExpressions;
using EmployeeSelfServiceApi.DTOs.Common;
using EmployeeSelfServiceApi.Exceptions;
using EmployeeServiceApi.DTOs.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceApi.Middleware;

/// <summary>
/// Converts every exception into the agreed error wrapper
/// { responseCode, responseMessage, errors: [{ field, message }] }.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    // InvalidOperationException is used by the services for both
    // "invalid state / duplicate" (409) and "bad input" (400).
    // Messages that describe bad input are mapped to 400.
    private static readonly Regex ValidationMessage = new(
        @"\b(required|must|negative|greater than|cannot be before|on or before|on or after|" +
        @"do not match|invalid|at least|exactly|in UTC|between|future|no more than|missing|" +
        @"valid)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; nothing to write.
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var response = new ApiErrorResponseDto();

        switch (exception)
        {
            case ApiException apiException:
                response.ResponseCode = apiException.StatusCode;
                response.ResponseMessage = apiException.Message;
                response.Errors = apiException.Errors;
                break;

            case ValidationException validationException:
                response.ResponseCode = StatusCodes.Status400BadRequest;
                response.ResponseMessage = "Validation failed.";
                response.Errors = validationException.Errors
                    .Select(error => new ApiErrorDto
                    {
                        Field = error.PropertyName,
                        Message = error.ErrorMessage
                    })
                    .ToList();
                break;

            case KeyNotFoundException:
                response.ResponseCode = StatusCodes.Status404NotFound;
                response.ResponseMessage = exception.Message;
                break;

            case UnauthorizedAccessException:
                response.ResponseCode = StatusCodes.Status403Forbidden;
                response.ResponseMessage = exception.Message;
                break;

            case ArgumentException argumentException:
                response.ResponseCode = StatusCodes.Status400BadRequest;
                response.ResponseMessage = argumentException.Message;
                response.Errors.Add(new ApiErrorDto
                {
                    Field = argumentException.ParamName ?? string.Empty,
                    Message = argumentException.Message
                });
                break;

            case InvalidOperationException:
                response.ResponseCode = ValidationMessage.IsMatch(exception.Message)
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status409Conflict;
                response.ResponseMessage = exception.Message;
                break;

            case DbUpdateConcurrencyException:
                response.ResponseCode = StatusCodes.Status409Conflict;
                response.ResponseMessage =
                    "The record was changed by another request. Refresh and try again.";
                break;

            case DbUpdateException dbUpdateException:
                // Unique-index violations (duplicates, double approvals).
                _logger.LogWarning(dbUpdateException,
                    "Database update failed for {Path}.", context.Request.Path);
                response.ResponseCode = StatusCodes.Status409Conflict;
                response.ResponseMessage =
                    "The request conflicts with existing data (for example a duplicate).";
                break;

            case BadHttpRequestException badRequest:
                response.ResponseCode = StatusCodes.Status400BadRequest;
                response.ResponseMessage = "Invalid request.";
                response.Errors.Add(new ApiErrorDto
                {
                    Field = string.Empty,
                    Message = badRequest.Message
                });
                break;

            case JsonException jsonException:
                response.ResponseCode = StatusCodes.Status400BadRequest;
                response.ResponseMessage = "The request body is not valid JSON.";
                response.Errors.Add(new ApiErrorDto
                {
                    Field = jsonException.Path ?? string.Empty,
                    Message = jsonException.Message
                });
                break;

            default:
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while processing {Path}.",
                    context.Request.Path);

                response.ResponseCode = StatusCodes.Status500InternalServerError;
                response.ResponseMessage = "An unexpected error occurred.";
                break;
        }

        context.Response.Clear();
        context.Response.StatusCode = response.ResponseCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions));
    }
}
