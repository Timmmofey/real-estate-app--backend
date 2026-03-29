using Classified.Shared.Constants;
using System.Text.Json.Serialization;

namespace AuthService.Domain.DTOs
{
    public record SessionResponseDto
    {
        public required Guid SessionId { get; set; }
        public required string DeviceName { get; set; }
        public bool IsCurrentSession { get; set; }
        public DateTime LastActivity {  get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType? DeviceType { get; set;} = null;
        public string? IpAddress { get; set; } = null;
        public string? Country { get; set; } = null;
        public string? Settlement { get; set; } = null;
    }
}