using System.Text;
using System.Text.Json;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class EasyAuthCurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IBookingRepository<AppUser> users) : ICurrentUserService
{
    public CurrentUserInfo? GetCurrentPrincipal()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return null;
        }

        var provider = request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].ToString();
        var providerUserId = request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].ToString();
        var name = request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].ToString();
        var email = name.Contains('@') ? name : string.Empty;

        var encodedPrincipal = request.Headers["X-MS-CLIENT-PRINCIPAL"].ToString();
        if (!string.IsNullOrWhiteSpace(encodedPrincipal))
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
            var clientPrincipal = JsonSerializer.Deserialize<ClientPrincipal>(decoded, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            email = clientPrincipal?.Claims?.FirstOrDefault(claim =>
                claim.Type.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase) ||
                claim.Type.Equals("email", StringComparison.OrdinalIgnoreCase))?.Value ?? email;
        }

        return string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId)
            ? null
            : new CurrentUserInfo(provider, providerUserId, name, email);
    }

    public async Task<AppUser?> GetCurrentAppUserAsync(CancellationToken cancellationToken)
    {
        var principal = GetCurrentPrincipal();
        return principal is null ? null : await users.GetAsync(principal.LocalUserId, cancellationToken);
    }

    private sealed class ClientPrincipal
    {
        public List<ClientPrincipalClaim> Claims { get; set; } = [];
    }

    private sealed class ClientPrincipalClaim
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
