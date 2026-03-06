namespace Classified.Shared.Libs
{
    public class StringHelper
    {
        public static string ToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
                return value;

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }
}
