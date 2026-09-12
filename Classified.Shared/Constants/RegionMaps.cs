namespace Classified.Shared.Constants
{
    //public static class RegionMaps
    //{
    //    private static readonly Dictionary<string, string> Countries = new()
    //    {
    //        {"US","United States"},
    //        {"CA","Canada"},
    //    };

    //    private static readonly Dictionary<string, Dictionary<string, string>> Regions = new()
    //    {
    //        ["US"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //        {
    //            ["AL"] = "Alabama",
    //            ["AK"] = "Alaska",
    //            ["AZ"] = "Arizona",
    //            ["AR"] = "Arkansas",
    //            ["CA"] = "California",
    //            ["CO"] = "Colorado",
    //            ["CT"] = "Connecticut",
    //            ["DE"] = "Delaware",
    //            ["FL"] = "Florida",
    //            ["GA"] = "Georgia",
    //            ["HI"] = "Hawaii",
    //            ["ID"] = "Idaho",
    //            ["IL"] = "Illinois",
    //            ["IN"] = "Indiana",
    //            ["IA"] = "Iowa",
    //            ["KS"] = "Kansas",
    //            ["KY"] = "Kentucky",
    //            ["LA"] = "Louisiana",
    //            ["ME"] = "Maine",
    //            ["MD"] = "Maryland",
    //            ["MA"] = "Massachusetts",
    //            ["MI"] = "Michigan",
    //            ["MN"] = "Minnesota",
    //            ["MS"] = "Mississippi",
    //            ["MO"] = "Missouri",
    //            ["MT"] = "Montana",
    //            ["NE"] = "Nebraska",
    //            ["NV"] = "Nevada",
    //            ["NH"] = "New Hampshire",
    //            ["NJ"] = "New Jersey",
    //            ["NM"] = "New Mexico",
    //            ["NY"] = "New York",
    //            ["NC"] = "North Carolina",
    //            ["ND"] = "North Dakota",
    //            ["OH"] = "Ohio",
    //            ["OK"] = "Oklahoma",
    //            ["OR"] = "Oregon",
    //            ["PA"] = "Pennsylvania",
    //            ["RI"] = "Rhode Island",
    //            ["SC"] = "South Carolina",
    //            ["SD"] = "South Dakota",
    //            ["TN"] = "Tennessee",
    //            ["TX"] = "Texas",
    //            ["UT"] = "Utah",
    //            ["VT"] = "Vermont",
    //            ["VA"] = "Virginia",
    //            ["WA"] = "Washington",
    //            ["WV"] = "West Virginia",
    //            ["WI"] = "Wisconsin",
    //            ["WY"] = "Wyoming"
    //        },
    //        ["CA"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //        {
    //            ["AB"] = "Alberta",
    //            ["BC"] = "British Columbia",
    //            ["MB"] = "Manitoba",
    //            ["NB"] = "New Brunswick",
    //            ["NL"] = "Newfoundland and Labrador",
    //            ["NS"] = "Nova Scotia",
    //            ["ON"] = "Ontario",
    //            ["PE"] = "Prince Edward Island",
    //            ["QC"] = "Quebec",
    //            ["SK"] = "Saskatchewan",
    //            ["NT"] = "Northwest Territories",
    //            ["NU"] = "Nunavut",
    //            ["YT"] = "Yukon"
    //        }
    //    };

    //    public static bool IsCountryAllowed(string code) => Countries.ContainsKey(code.ToUpperInvariant());
    //    public static bool IsRegionAllowed(string country, string regionCode) =>
    //        Regions.TryGetValue(country.ToUpperInvariant(), out var dict) && dict.ContainsKey(regionCode.ToUpperInvariant());

    //    public static bool IsCountryOrRegionAllowed(string country, string? regionCode = null)
    //    {
    //        if (regionCode == null) return IsCountryAllowed(country);

    //        return Regions.TryGetValue(country.ToUpperInvariant(), out var dict) && dict.ContainsKey(regionCode.ToUpperInvariant());
    //    }
    //    public static string? GetRegionName(string country, string regionCode)
    //    {
    //        if (string.IsNullOrEmpty(regionCode)) return null;
    //        return Regions.TryGetValue(country.ToUpperInvariant(), out var dict) && dict.TryGetValue(regionCode.ToUpperInvariant(), out var name) ? name : null;
    //    }
    //    public static string? GetCountryName(string code) => Countries.TryGetValue(code.ToUpperInvariant(), out var n) ? n : null;


    //}

    public static class RegionMaps
    {
        private static readonly Dictionary<CountryCode, string> Countries = new()
        {
            [CountryCode.US] = "United States",
            [CountryCode.CA] = "Canada",
        };

        private static readonly Dictionary<CountryCode, Dictionary<string, string>> Regions = new()
        {
            [CountryCode.US] = new()
            {
                ["AL"] = "Alabama",
                ["AK"] = "Alaska",
                ["AZ"] = "Arizona",
                ["AR"] = "Arkansas",
                ["CA"] = "California",
                ["CO"] = "Colorado",
                ["CT"] = "Connecticut",
                ["DE"] = "Delaware",
                ["FL"] = "Florida",
                ["GA"] = "Georgia",
                ["HI"] = "Hawaii",
                ["ID"] = "Idaho",
                ["IL"] = "Illinois",
                ["IN"] = "Indiana",
                ["IA"] = "Iowa",
                ["KS"] = "Kansas",
                ["KY"] = "Kentucky",
                ["LA"] = "Louisiana",
                ["ME"] = "Maine",
                ["MD"] = "Maryland",
                ["MA"] = "Massachusetts",
                ["MI"] = "Michigan",
                ["MN"] = "Minnesota",
                ["MS"] = "Mississippi",
                ["MO"] = "Missouri",
                ["MT"] = "Montana",
                ["NE"] = "Nebraska",
                ["NV"] = "Nevada",
                ["NH"] = "New Hampshire",
                ["NJ"] = "New Jersey",
                ["NM"] = "New Mexico",
                ["NY"] = "New York",
                ["NC"] = "North Carolina",
                ["ND"] = "North Dakota",
                ["OH"] = "Ohio",
                ["OK"] = "Oklahoma",
                ["OR"] = "Oregon",
                ["PA"] = "Pennsylvania",
                ["RI"] = "Rhode Island",
                ["SC"] = "South Carolina",
                ["SD"] = "South Dakota",
                ["TN"] = "Tennessee",
                ["TX"] = "Texas",
                ["UT"] = "Utah",
                ["VT"] = "Vermont",
                ["VA"] = "Virginia",
                ["WA"] = "Washington",
                ["WV"] = "West Virginia",
                ["WI"] = "Wisconsin",
                ["WY"] = "Wyoming"
            },

            //[CountryCode.CA] = new(StringComparer.OrdinalIgnoreCase)

            [CountryCode.CA] = new()
            {
                ["AB"] = "Alberta",
                ["BC"] = "British Columbia",
                ["MB"] = "Manitoba",
                ["NB"] = "New Brunswick",
                ["NL"] = "Newfoundland and Labrador",
                ["NS"] = "Nova Scotia",
                ["ON"] = "Ontario",
                ["PE"] = "Prince Edward Island",
                ["QC"] = "Quebec",
                ["SK"] = "Saskatchewan",
                ["NT"] = "Northwest Territories",
                ["NU"] = "Nunavut",
                ["YT"] = "Yukon"
            }
        };

        public static bool IsCountryAllowed(string? country)
        {
            return TryParseCountry(country, out var countryCode) &&
                   Countries.ContainsKey(countryCode);
        }

        public static bool IsRegionAllowed(string? country, string? regionCode)
        {
            return TryParseCountry(country, out var countryCode) &&
                   !string.IsNullOrWhiteSpace(regionCode) &&
                   Regions.TryGetValue(countryCode, out var regions) &&
                   regions.ContainsKey(regionCode);
        }

        public static bool IsCountryOrRegionAllowed(string? country, string? regionCode = null)
        {
            if (!TryParseCountry(country, out var countryCode))
                return false;

            return string.IsNullOrWhiteSpace(regionCode) ||
                   IsRegionAllowed(countryCode.ToString(), regionCode);
        }

        public static string? GetRegionName(string? country, string? regionCode)
        {
            if (!TryParseCountry(country, out var countryCode) ||
                string.IsNullOrWhiteSpace(regionCode))
                return null;

            return Regions.TryGetValue(countryCode, out var regions) &&
                   regions.TryGetValue(regionCode, out var name)
                ? name
                : null;
        }

        public static string? GetCountryName(string? country)
        {
            return TryParseCountry(country, out var countryCode) &&
                   Countries.TryGetValue(countryCode, out var name)
                ? name
                : null;
        }

        private static bool TryParseCountry(string? value, out CountryCode country)
        {
            return Enum.TryParse(value, ignoreCase: true, out country);
        }

    }

}
