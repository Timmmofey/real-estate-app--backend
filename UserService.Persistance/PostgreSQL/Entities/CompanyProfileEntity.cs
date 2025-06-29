namespace UserService.Persistance.PostgreSQL.Entities
{
    public class CompanyProfileEntity
    {
        public Guid UserId { get; set;}
        public string Name { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Settlement { get; set; } = default!;
        public string ZipCode { get; set; } = default!;
        public string RegistrationAdress { get; set; } = default!;
        public string СompanyRegistrationNumber { get; set; } = default!;
        public DateOnly EstimatedAt { get; set; }
        public string? MainPhotoUrl { get; set; }
        public string? Description { get; set; }

        public UserEntity User { get; set; } = default!;
    }
}
