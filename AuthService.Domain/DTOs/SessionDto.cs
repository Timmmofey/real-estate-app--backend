using Classified.Shared.Constants;
using System.Text.Json.Serialization;

namespace AuthService.Domain.DTOs
{
    public class SessionDto
    {
        public Guid SessionId { get; set; } = default!;
        public string DeviceName { get; set; } = default!;
        public bool IsCurrentSession { get; set; }
        public DateTime LastActivity {  get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DeviceType? DeviceType { get; set;} = null;
        public string? IpAddress { get; set; } = null;
        public string? Country { get; set; } = null;
        public string? Settlement { get; set; } = null;


    }
}