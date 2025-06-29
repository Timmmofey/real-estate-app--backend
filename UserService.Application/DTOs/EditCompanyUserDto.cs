namespace UserService.Application.DTOs
{
    public class EditCompanyUserDto
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Settlement { get; set; }
        public string? ZipCode { get; set; }
        public string? RegistrationAdress { get; set; }
        public string? СompanyRegistrationNumber { get; set; }
        public DateOnly? EstimatedAt { get; set; }
        public string? Description { get; set; }
    }
}
