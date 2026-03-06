using System;
namespace Classified.Shared.Extensions.ErrorHandler.Errors
{
    public sealed class ApiErrorResponse
    {
        public string Message { get; init; } = default!;
        public Dictionary<string, string[]>? Errors { get; init; } 
        public string? Code { get; init; }
        public string? TraceId { get; init; }
    }
}
