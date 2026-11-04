namespace Classified.Shared.Libs.CodeHasher
{
    public sealed class CodeHasherOptions
    {
        public const string SectionName = "CodeHasher";

        public string Secret { get; set; } = string.Empty;
    }
}
