using UserService.Domain.Models;

public class PersonProfile
{
    public Guid UserId { get; }
    public string FirstName { get; } = default!;
    public string LastName { get; } = default!;
    public string? MainPhotoUrl { get; }
    public string? Country { get; }
    public string? Region { get; }
    public string? Settlement { get; }
    public string? ZipCode { get; }

    public User User { get; } = default!;

    private PersonProfile(Guid userId, string firstName, string lastName, string? mainPhotoUrl, string? сountry, string? region, string? settlement, string? zipCode)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        MainPhotoUrl = mainPhotoUrl;
        Country = сountry;
        Region = region;
        Settlement = settlement;
        ZipCode = zipCode;
    }

    public static (PersonProfile? Profile, string? Error) Create(Guid userId, string firstName, string lastName, string? mainPhotoUrl, string? сountry, string? region, string? settlement, string? zipCode)
    {
        if (userId == Guid.Empty)
            return (null, "UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2 || firstName.Length > 50)
            return (null, "First name must be between 2 and 50 characters.");

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2 || lastName.Length > 50)
            return (null, "Last name must be between 2 and 50 characters.");
        
        if(!string.IsNullOrWhiteSpace(region) && string.IsNullOrWhiteSpace(сountry))
            return (null, "You cant enter a region without entering a country");

        if (!string.IsNullOrWhiteSpace(settlement) && (string.IsNullOrWhiteSpace(сountry) || string.IsNullOrWhiteSpace(region)))
            return (null, "You cant enter a settlement without entering a country and region");

        if (!string.IsNullOrWhiteSpace(zipCode) && (string.IsNullOrWhiteSpace(сountry) && !string.IsNullOrWhiteSpace(region) && string.IsNullOrWhiteSpace(settlement)))
            return (null, "You cant enter a zip code without entering a country, region and settlement");


        var profile = new PersonProfile(userId, firstName, lastName, mainPhotoUrl, сountry, region, settlement, zipCode);
        return (profile, null);
    }
}
