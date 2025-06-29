namespace UserService.Domain.Models
{
    public class CompanyProfile
    {
        public Guid UserId { get; }
        public string Name { get; }
        public string Country { get; }
        public string Region { get; }
        public string Settlement { get; }
        public string ZipCode { get; }
        public string RegistrationAdress { get; }
        public string СompanyRegistrationNumber { get; }
        public DateOnly EstimatedAt { get; }
        public string? MainPhotoUrl { get; }
        public string? Description { get; }

        private CompanyProfile(Guid userId, string name, string сountry, string region, string settlement, string registrationAdress, string zipCode, string сompanyRegistrationNumber, DateOnly estimatedAt, string? mainPhotoUrl, string? description)
        {
            UserId = userId;
            Name = name;
            Country = сountry;
            Region = region;
            Settlement = settlement;
            RegistrationAdress = registrationAdress;
            ZipCode = zipCode;
            СompanyRegistrationNumber = сompanyRegistrationNumber; 
            EstimatedAt = estimatedAt;
            MainPhotoUrl = mainPhotoUrl;
            Description = description;
        }

        public static (CompanyProfile? Profile, string? Error) Create(
            Guid userId,
            string name,
            string сountry,
            string region,
            string settlement,
            string zipCode,
            string registrationAdress,
            string сompanyRegistrationNumber,
            DateOnly estimatedAt,
            string? mainPhotoUrl,
            string? description)
        {
            if (userId == Guid.Empty)
                return (null, "UserId cannot be empty.");

            if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 100)
                return (null, "Company name must be between 3 and 100 characters.");

            if (string.IsNullOrWhiteSpace(registrationAdress) || registrationAdress.Length < 5 || registrationAdress.Length > 150)
                return (null, "Company address must be between 5 and 150 characters.");

            if (string.IsNullOrWhiteSpace(сompanyRegistrationNumber))
                return (null, "EIN cannot be empty.");

            // Удалим тире, если есть
            сompanyRegistrationNumber = сompanyRegistrationNumber.Replace("-", "");

            if (сompanyRegistrationNumber.Length != 9 || !сompanyRegistrationNumber.All(char.IsDigit))
                return (null, "EIN must be exactly 9 digits (with or without dashes).");

            if (estimatedAt > DateOnly.FromDateTime(DateTime.UtcNow))
                return (null, "EstimatedAt cannot be in the future.");

            if (!string.IsNullOrEmpty(mainPhotoUrl) && !Uri.IsWellFormedUriString(mainPhotoUrl, UriKind.Absolute))
                return (null, "Company main photo URL is invalid.");

            if (!string.IsNullOrEmpty(description) && description.Length > 500)
                return (null, "Description cannot be longer than 500 characters.");

            var profile = new CompanyProfile(userId, name, сountry, region, settlement, zipCode, registrationAdress, сompanyRegistrationNumber, estimatedAt, mainPhotoUrl, description);
            return (profile, null);
        }
    }
}