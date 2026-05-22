using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class EasyAuthClaimsMiddleware
    {
        private readonly RequestDelegate _next;

        public EasyAuthClaimsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var provider = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].ToString();
            var providerUserId = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].ToString();

            if (!string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(providerUserId))
            {
                var name = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].ToString();
                var email = ReadEmailFromPrincipalHeader(context) ?? (name.Contains('@') ? name : string.Empty);

                var claims = new List<Claim>
                {
                    new Claim(EasyAuthClaimTypes.Provider, provider),
                    new Claim(EasyAuthClaimTypes.ProviderUserId, providerUserId),
                    new Claim(ClaimTypes.NameIdentifier, $"{provider}-{providerUserId}".Replace("|", "-", StringComparison.Ordinal)),
                    new Claim(ClaimTypes.Name, name)
                };

                if (!string.IsNullOrWhiteSpace(email))
                {
                    claims.Add(new Claim(EasyAuthClaimTypes.Email, email));
                    claims.Add(new Claim(ClaimTypes.Email, email));
                }

                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AzureAppServiceAuthentication"));
            }

            await _next(context);
        }

        private static string? ReadEmailFromPrincipalHeader(HttpContext context)
        {
            var encodedPrincipal = context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].ToString();
            if (string.IsNullOrWhiteSpace(encodedPrincipal))
            {
                return null;
            }

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
                var clientPrincipal = JsonSerializer.Deserialize<ClientPrincipal>(decoded, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return clientPrincipal?.Claims?.FirstOrDefault(claim =>
                    claim.Type.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase) ||
                    claim.Type.Equals("email", StringComparison.OrdinalIgnoreCase))?.Value;
            }
            catch (FormatException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed class ClientPrincipal
        {
            public List<ClientPrincipalClaim> Claims { get; set; } = new List<ClientPrincipalClaim>();
        }

        private sealed class ClientPrincipalClaim
        {
            public string Type { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}
