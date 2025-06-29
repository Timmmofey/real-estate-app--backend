namespace Classified.Shared.Infrastructure.S3.Configuration
{
    public class S3Options
    {
        public string AccessKey { get; set; } = default!;
        public string SecretKey { get; set; } = default!;
        public string ServiceUrl { get; set; } = default!;
        public string BucketName { get; set; } = default!;
    }
}
