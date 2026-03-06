using Classified.Shared.Extensions.ErrorHandler.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Classified.Shared.Extensions.ErrorHandler
{
    public static class ApiErrorHandlingExtensions
    {
        public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(kvp => kvp.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors
                                      .Select(e => e.ErrorMessage ?? e.Exception?.Message ?? "Invalid")
                                      .ToArray()
                        );

                    var traceId = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                    var apiError = new ApiErrorResponse
                    {
                        Message = "Validation failed",
                        Errors = errors,
                        Code = "VALIDATION_ERROR",
                        TraceId = traceId
                    };

                    return new BadRequestObjectResult(apiError);
                };
            });

            return services;
        }
    }
}
