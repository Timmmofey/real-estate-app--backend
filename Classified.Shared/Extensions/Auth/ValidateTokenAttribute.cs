using Classified.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace Classified.Shared.Extensions.Auth
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateTokenAttribute : Attribute, IAuthorizationFilter
    {
        private readonly JwtTokenType _tokenType;
        
        public ValidateTokenAttribute(JwtTokenType tokenType)
        {
            _tokenType = tokenType;
        }

        //public void OnAuthorization(AuthorizationFilterContext context)
        //{
        //    var httpContext = context.HttpContext;

        //    var tokenValidator = httpContext.RequestServices.GetService(typeof(ITokenValidationService)) as ITokenValidationService;
        //    if (tokenValidator == null)
        //    {
        //        context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //        return;
        //    }

        //    var principal = tokenValidator.ValidateTokenByType(_tokenType, httpContext.Request.Cookies);
        //    if (principal == null)
        //    {
        //        context.Result = new UnauthorizedResult();
        //        return;
        //    }

        //    // Optionally set validated principal to HttpContext.User
        //    httpContext.User = principal;
        //}

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;

            // Получаем сервис валидации токена
            var tokenValidator = httpContext.RequestServices.GetService(typeof(ITokenValidationService)) as ITokenValidationService;
            if (tokenValidator == null)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            // Определяем имя куки по типу токена
            var cookieName = CookieNameByType(_tokenType);

            // Проверяем наличие куки
            if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var rawToken) || string.IsNullOrEmpty(rawToken))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            try
            {
                // Валидируем токен: подпись, срок жизни и claim type
                var principal = tokenValidator.ValidateAndGetPrincipal(rawToken, _tokenType);

                // Подставляем валидированный principal в HttpContext.User
                httpContext.User = principal;
            }
            catch (SecurityTokenException)
            {
                // Токен невалиден: подпись, lifetime или неверный type
                context.Result = new UnauthorizedResult();
                return;
            }
            catch (Exception)
            {
                // Любая другая ошибка — тоже 401
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        private static string CookieNameByType(JwtTokenType type) => type switch
        {
            JwtTokenType.Access => CookieNames.Auth,
            JwtTokenType.Refresh => CookieNames.Refresh,
            JwtTokenType.Device => CookieNames.Device,
            JwtTokenType.Restore => CookieNames.Restore,
            JwtTokenType.PasswordReset => CookieNames.PasswordReset,
            JwtTokenType.RequestNewEmailCofirmation => CookieNames.RequestNewEmailCofirmation,
            JwtTokenType.EmailReset => CookieNames.EmailReset,
            JwtTokenType.TwoFactorAuthentication => CookieNames.TwoFactorAuthentication,
            JwtTokenType.OAuthRegistration => CookieNames.OAuthRegistration,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
