namespace UserService.Persistance.PostgreSQL.Entities
{
    public class CompanyProfileEntity
    {
        public Guid UserId { get; set;}
        public required string Name { get; set; }
        public required string Country { get; set; } 
        public required string Region { get; set; } 
        public required string Settlement { get; set; } 
        public required string ZipCode { get; set; } 
        public required string RegistrationAdress { get; set; } 
        public required string СompanyRegistrationNumber { get; set; } 
        public DateOnly EstimatedAt { get; set; }
        public string? MainPhotoUrl { get; set; }
        public string? Description { get; set; }

        public UserEntity User { get; set; } = default!;
    }
}
