namespace UserService.Persistance.PostgreSQL.Entities
{
    public class PersonProfileEntity
    {
        public Guid UserId { get; set; }
        public required string FirstName { get; set; } 
        public required string LastName { get; set; }
        public string? MainPhotoUrl { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }

        public UserEntity User { get; set; } = default!;
    }
}
