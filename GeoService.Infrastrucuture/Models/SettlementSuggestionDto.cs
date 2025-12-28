namespace GeoService.Domain.Models
{
    public class SettlementSuggestionDto
    {
        public string Settlement { get; set; } = "";
        public Dictionary<string, string> Other_Settlement_Names { get; set; } = new();

        public string DisplayName { get; set; } = "";
        public Dictionary<string, string> Other_DisplayName_Names { get; set; } = new();
    }

}
