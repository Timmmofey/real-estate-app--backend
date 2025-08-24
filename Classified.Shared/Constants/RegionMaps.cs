namespace Classified.Shared.Constants
{
    public static class RegionMaps
    {
        private static readonly Dictionary<string, string> Countries = new()
    {
        {"US","United States"},
        {"CA","Canada"},
    };

        private static readonly Dictionary<string, Dictionary<string, string>> Regions = new()
        {
            ["US"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            ["CA"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        public static bool IsCountryAllowed(string code) => Countries.ContainsKey(code.ToUpperInvariant());
        public static bool IsRegionAllowed(string country, string regionCode) =>
            Regions.TryGetValue(country.ToUpperInvariant(), out var dict) && dict.ContainsKey(regionCode.ToUpperInvariant());
        public static string? GetRegionName(string country, string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode)) return null;
            return Regions.TryGetValue(country.ToUpperInvariant(), out var dict) && dict.TryGetValue(regionCode.ToUpperInvariant(), out var name) ? name : null;
        }
        public static string? GetCountryName(string code) => Countries.TryGetValue(code.ToUpperInvariant(), out var n) ? n : null;


    }

}
