namespace UserService.Application.DTOs
{
    public class PersonUserProfileResponseDto
    {
        public required string FirstName { get; set; } 
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumer { get; set; }
        public bool IsVerified { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public bool IsOAuthOnly { get; set; }
        public string? MainPhotoUrl { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }

    }
}
