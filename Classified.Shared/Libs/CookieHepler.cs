using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace Classified.Shared.Functions
{
    public static class CookieHepler
    {
        public static void SetCookie(this HttpResponse response, string name, string value, int? days = null, int? minutes = null)
        {
            var expires = DateTimeOffset.UtcNow;
            if (days.HasValue)
                expires = expires.AddDays(days.Value);
            else if (minutes.HasValue)
                expires = expires.AddMinutes(minutes.Value);

            response.Cookies.Append(name, value, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                Expires = expires
            });
        }

        public static void DeleteCookie(HttpResponse response, string name, bool secure = true)
        {
            SetCookie(response, name, "", days: 1);
        }

        public static void RemoveRefreshAuthDeviceTokens(HttpResponse response)
        {
            DeleteCookie(response, CookieNames.Auth.ToString());
            DeleteCookie(response, CookieNames.Refresh.ToString());
            DeleteCookie(response, CookieNames.Device.ToString());
        }
    }
}
