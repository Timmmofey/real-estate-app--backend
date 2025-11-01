using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Classified.Shared.Extensions
{
    public static class LocalizationExtensions
    {
        public static IServiceCollection AddAppLocalization(this IServiceCollection services)
        {
            services.AddLocalization();

            var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ru") };

            var requestLocalizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            requestLocalizationOptions.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());

            services.Configure<RequestLocalizationOptions>(opts =>
            {
                opts.DefaultRequestCulture = requestLocalizationOptions.DefaultRequestCulture;
                opts.SupportedCultures = requestLocalizationOptions.SupportedCultures;
                opts.SupportedUICultures = requestLocalizationOptions.SupportedUICultures;
                opts.RequestCultureProviders = requestLocalizationOptions.RequestCultureProviders;
            });

            return services;
        }

        // Включает middleware локализации (для IApplicationBuilder)
        public static IApplicationBuilder UseAppLocalization(this IApplicationBuilder app)
        {
            var opts = app.ApplicationServices.GetService(typeof(IOptions<RequestLocalizationOptions>))
                       as IOptions<RequestLocalizationOptions>;

            if (opts?.Value != null)
            {
                app.UseRequestLocalization(opts.Value);
            }
            else
            {
                // fallback
                var fallbackCultures = new[] { new CultureInfo("en"), new CultureInfo("ru") };
                var fallback = new RequestLocalizationOptions
                {
                    DefaultRequestCulture = new RequestCulture("en"),
                    SupportedCultures = fallbackCultures,
                    SupportedUICultures = fallbackCultures
                };
                fallback.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
                app.UseRequestLocalization(fallback);
            }

            return app;
        }

        // Overload для WebApplication — НИ В КОЕМ СЛУЧАЕ НЕ ВЫЗЫВАЕМ app.UseAppLocalization() напрямую,
        // вместо этого явно используем реализацию для IApplicationBuilder (чтобы избежать рекурсии).
        public static WebApplication UseAppLocalization(this WebApplication app)
        {
            // Явно вызываем IApplicationBuilder-версию (она возвращает IApplicationBuilder),
            // затем возвращаем тот же WebApplication для цепочки вызовов.
            ((IApplicationBuilder)app).UseAppLocalization();
            return app;
        }
    }
}
