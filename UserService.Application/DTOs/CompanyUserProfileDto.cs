namespace UserService.Application.DTOs
{
    public class CompanyUserProfileDto
    {
        public string Name { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Settlement { get; set; } = default!;
        public string ZipCode { get; set; } = default!;
        public string RegistrationAdress { get; set; } = default!;
        public string СompanyRegistrationNumber { get; set; } = default!;
        public DateOnly EstimatedAt { get; set; } = default!;
        public string? MainPhotoUrl { get; set; }
        public string? Description { get; set; }
    }
}
