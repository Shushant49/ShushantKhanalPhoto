using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ShushantKhanalPhoto.Filters
{
    // Guards admin-only endpoints: the request must send X-Api-Key matching Admin:ApiKey in config
    public class RequireApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        private const string HeaderName = "X-Api-Key";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedKey = config["Admin:ApiKey"];

            // No key configured = endpoint locked, so a missing setting never leaves it open
            if (string.IsNullOrEmpty(expectedKey) ||
                !context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(providedKey.ToString()),
                    Encoding.UTF8.GetBytes(expectedKey)))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
