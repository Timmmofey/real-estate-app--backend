namespace GeoService.Domain.Models
{
    public static class GeoConstants
    {
        public static readonly Country[] Countries =
        {
            new("US", "United States"),
            new("CA", "Canada")
        };

        public static readonly Dictionary<string, Region[]> RegionsByCountry =
            new()
            {
                ["US"] = new[]
                {
                    new Region("AL", "Alabama"),
                    new Region("AK", "Alaska"),
                    new Region("AZ", "Arizona"),
                    new Region("AR", "Arkansas"),
                    new Region("CA", "California"),
                    new Region("CO", "Colorado"),
                    new Region("CT", "Connecticut"),
                    new Region("DE", "Delaware"),
                    new Region("FL", "Florida"),
                    new Region("GA", "Georgia"),
                    new Region("HI", "Hawaii"),
                    new Region("ID", "Idaho"),
                    new Region("IL", "Illinois"),
                    new Region("IN", "Indiana"),
                    new Region("IA", "Iowa"),
                    new Region("KS", "Kansas"),
                    new Region("KY", "Kentucky"),
                    new Region("LA", "Louisiana"),
                    new Region("ME", "Maine"),
                    new Region("MD", "Maryland"),
                    new Region("MA", "Massachusetts"),
                    new Region("MI", "Michigan"),
                    new Region("MN", "Minnesota"),
                    new Region("MS", "Mississippi"),
                    new Region("MO", "Missouri"),
                    new Region("MT", "Montana"),
                    new Region("NE", "Nebraska"),
                    new Region("NV", "Nevada"),
                    new Region("NH", "New Hampshire"),
                    new Region("NJ", "New Jersey"),
                    new Region("NM", "New Mexico"),
                    new Region("NY", "New York"),
                    new Region("NC", "North Carolina"),
                    new Region("ND", "North Dakota"),
                    new Region("OH", "Ohio"),
                    new Region("OK", "Oklahoma"),
                    new Region("OR", "Oregon"),
                    new Region("PA", "Pennsylvania"),
                    new Region("RI", "Rhode Island"),
                    new Region("SC", "South Carolina"),
                    new Region("SD", "South Dakota"),
                    new Region("TN", "Tennessee"),
                    new Region("TX", "Texas"),
                    new Region("UT", "Utah"),
                    new Region("VT", "Vermont"),
                    new Region("VA", "Virginia"),
                    new Region("WA", "Washington"),
                    new Region("WV", "West Virginia"),
                    new Region("WI", "Wisconsin"),
                    new Region("WY", "Wyoming")
                },
                ["CA"] = new[]
                {
                    new Region("AB", "Alberta"),
                    new Region("BC", "British Columbia"),
                    new Region("MB", "Manitoba"),
                    new Region("NB", "New Brunswick"),
                    new Region("NL", "Newfoundland and Labrador"),
                    new Region("NS", "Nova Scotia"),
                    new Region("ON", "Ontario"),
                    new Region("PE", "Prince Edward Island"),
                    new Region("QC", "Quebec"),
                    new Region("SK", "Saskatchewan"),
                    new Region("NT", "Northwest Territories"),
                    new Region("NU", "Nunavut"),
                    new Region("YT", "Yukon")
                }
            };
    }
}
