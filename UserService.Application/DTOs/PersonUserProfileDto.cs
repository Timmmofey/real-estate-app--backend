namespace UserService.Application.DTOs
{
    public class PersonUserProfileDto
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PhoneNumer { get; set; } = default!;
        public bool IsVerified { get; set; }
        public string? MainPhotoUrl { get; set; } 
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }

    }
}
