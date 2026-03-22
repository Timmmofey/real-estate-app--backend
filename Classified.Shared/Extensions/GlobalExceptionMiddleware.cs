using Classified.Shared.Extensions.ErrorHandler.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            ApiErrorResponse apiError;
            int statusCode;

            switch (ex)
            {
                case DomainValidationException dve:
                    statusCode = StatusCodes.Status400BadRequest;
                    apiError = new ApiErrorResponse
                    {
                        Message = dve.Message,
                        Errors = dve.Errors.ToDictionary(x => x.Key, x => x.Value),
                        Code = "VALIDATION_ERROR",
                        TraceId = traceId
                    };
                    break;

                case ValidationException fvEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    var errorsFromFluent = fvEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    apiError = new ApiErrorResponse
                    {
                        Message = fvEx.Message,
                        Errors = errorsFromFluent,
                        Code = "VALIDATION_ERROR",
                        TraceId = traceId
                    };
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    apiError = new ApiErrorResponse
                    {
                        Message = ex.Message,
                        Code = "UNAUTHORIZED",
                        TraceId = traceId
                    };
                    break;

                case KeyNotFoundException _:
                case NotFoundException _:
                    statusCode = StatusCodes.Status404NotFound;
                    apiError = new ApiErrorResponse
                    {
                        Message = ex.Message,
                        Code = "NOT_FOUND",
                        TraceId = traceId
                    };
                    break;

                case ArgumentException _:
                case NullReferenceException _:
                    statusCode = StatusCodes.Status400BadRequest;
                    apiError = new ApiErrorResponse
                    {
                        Message = ex.Message,
                        Code = "BAD_REQUEST",
                        TraceId = traceId
                    };
                    break;
                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    apiError = new ApiErrorResponse
                    {
                        Message = $"An unexpected error occurred: {ex.Message}",
                        Code = "INTERNAL_ERROR",
                        TraceId = traceId
                    };
                    _logger.LogError(ex, "Unhandled exception");
                    break;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(apiError, options);

            await context.Response.WriteAsync(json);
        }
    }
}