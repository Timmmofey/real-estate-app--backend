using Microsoft.Extensions.Configuration;

namespace Classified.Shared
{
    public static class LibraryConfiguration
    {
        public static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppContext.BaseDirectory) 
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets(typeof(LibraryConfiguration).Assembly, optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }
    }
}
