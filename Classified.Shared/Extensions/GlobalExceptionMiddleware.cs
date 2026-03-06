//using Classified.Shared.Extensions.ErrorHandler.Errors;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using System.Diagnostics;
//using System.Text.Json;

//namespace Classified.Shared.Extensions
//{
//    public class GlobalExceptionMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly ILogger<GlobalExceptionMiddleware> _logger;

//        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
//        {
//            _next = next;
//            _logger = logger;
//        }

//        //public async Task InvokeAsync(HttpContext context)
//        //{
//        //    try
//        //    {
//        //        await _next(context);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Unhandled exception occurred");

//        //        context.Response.ContentType = "application/json";
//        //        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

//        //        var response = new
//        //        {
//        //            error = ex.Message,
//        //            traceId = context.TraceIdentifier
//        //        };

//        //        var json = JsonSerializer.Serialize(response);
//        //        await context.Response.WriteAsync(json);
//        //    }
//        //}
//        //public async Task InvokeAsync(HttpContext context)
//        //{
//        //    try
//        //    {
//        //        await _next(context);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Unhandled exception occurred");

//        //        context.Response.ContentType = "application/json";

//        //        switch (ex)
//        //        {
//        //            case UnauthorizedAccessException:
//        //                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
//        //                break;
//        //            case ArgumentException:
//        //                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
//        //                break;
//        //            case KeyNotFoundException:
//        //                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
//        //                break;
//        //            default:
//        //                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
//        //                break;
//        //        }

//        //        var response = new
//        //        {
//        //            error = ex.Message,
//        //            traceId = context.TraceIdentifier
//        //        };

//        //        var json = JsonSerializer.Serialize(response);
//        //        await context.Response.WriteAsync(json);
//        //    }
//        //}

//        public async Task InvokeAsync(HttpContext context)
//        {
//            try
//            {
//                await _next(context);
//            }
//            catch (Exception ex)
//            {
//                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

//                ApiErrorResponse apiError;
//                int statusCode;

//                switch (ex)
//                {
//                    case DomainValidationException dve:
//                        statusCode = StatusCodes.Status400BadRequest;
//                        apiError = new ApiErrorResponse
//                        {
//                            Message = dve.Message,
//                            Errors = dve.Errors,
//                            Code = statusCode.ToString(),
//                            TraceId = traceId
//                        };
//                        break;

//                    case FluentValidation.ValidationException fvEx:
//                        statusCode = StatusCodes.Status400BadRequest;
//                        var errorsFromFluent = fvEx.Errors
//                            .GroupBy(e => e.PropertyName)
//                            .ToDictionary(
//                                g => g.Key,
//                                g => g.Select(e => e.ErrorMessage).ToArray()
//                            );

//                        apiError = new ApiErrorResponse
//                        {
//                            Message = "Validation failed",
//                            Errors = errorsFromFluent,
//                            Code = statusCode.ToString(),
//                            TraceId = traceId
//                        };
//                        break;

//                    case UnauthorizedAccessException:
//                        statusCode = StatusCodes.Status401Unauthorized;
//                        apiError = new ApiErrorResponse
//                        {
//                            Message = "Unauthorized",
//                            Code = statusCode.ToString(),
//                            TraceId = traceId
//                        };
//                        break;

//                    case KeyNotFoundException knf:
//                    case NotFoundException nf:
//                        statusCode = StatusCodes.Status404NotFound;
//                        apiError = new ApiErrorResponse
//                        {
//                            Message = ex.Message,
//                            Code = statusCode.ToString(),
//                            TraceId = traceId
//                        };
//                        break;

//                    case ArgumentException argEx:
//                        statusCode = StatusCodes.Status400BadRequest;
//                        apiError = new ApiErrorResponse
//                        {
//                            Message = argEx.Message,
//                            Code = statusCode.ToString(),
//                            TraceId = traceId
//                        };
//                        break;

//                    default:
//                        statusCode = StatusCodes.Status500InternalServerError;
//                        apiError = new ApiErrorResponse
//                        {
//                            Message = "An unexpected error occurred",
//                            Code = statusCode.ToString(),
//                            TraceId = traceId
//                        };
//                        _logger.LogError(ex, "Unhandled exception");
//                        break;
//                }

//                context.Response.StatusCode = statusCode;
//                context.Response.ContentType = "application/json";

//                var json = JsonSerializer.Serialize(apiError, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//                });

//                await context.Response.WriteAsync(json);
//            }
//        }


//    }
//}

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
                        Message = "Validation failed",
                        Errors = errorsFromFluent,
                        Code = "VALIDATION_ERROR",
                        TraceId = traceId
                    };
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    apiError = new ApiErrorResponse
                    {
                        Message = "Unauthorized",
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

                case ArgumentException argEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    apiError = new ApiErrorResponse
                    {
                        Message = argEx.Message,
                        Code = "BAD_REQUEST",
                        TraceId = traceId
                    };
                    break;

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    apiError = new ApiErrorResponse
                    {
                        Message = "An unexpected error occurred",
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