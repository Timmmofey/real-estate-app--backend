namespace UserService.Application.DTOs
{
    public record CompanyUserProfileResponseDto
    {
        public required string Name { get; set; } 
        public required string Email { get; set; } 
        public required string PhoneNumer { get; set; } 
        public bool IsVerified { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public bool IsOAuthOnly { get; set; }
        public required string Country { get; set; } 
        public required string Region { get; set; } 
        public required string Settlement { get; set; } 
        public required string ZipCode { get; set; } 
        public required string RegistrationAdress { get; set; } 
        public required string СompanyRegistrationNumber { get; set; } 
        public DateOnly EstimatedAt { get; set; } 
        public string? MainPhotoUrl { get; set; }
        public string? Description { get; set; }
    }
}
