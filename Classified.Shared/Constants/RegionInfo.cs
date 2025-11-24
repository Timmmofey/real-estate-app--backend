namespace Classified.Shared.Constants
{
    public class RegionInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public List<string> Aliases { get; set; } = new();


        public RegionInfo(string code, string name, IEnumerable<string> aliases)
        {
            Code = code;
            Name = name;
            Aliases = aliases.ToList();
        }


    }
}
