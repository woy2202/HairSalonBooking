using HairSalon.Booking.Api.Options;
using Microsoft.Extensions.Options;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class EasyAuthGuardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOptions<AzureBookingOptions> _options;

        public EasyAuthGuardMiddleware(RequestDelegate next, IOptions<AzureBookingOptions> options)
        {
            _next = next;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!ShouldCheckRequest(context))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-ID") ||
                !context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-IDP"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Brakuje danych zalogowanego użytkownika z Azure App Service Authentication. Sprawdź, czy Easy Auth jest włączone i wymaga logowania użytkownika."
                });
                return;
            }

            await _next(context);
        }

        private bool ShouldCheckRequest(HttpContext context)
        {
            if (!_options.Value.Security.RequireEasyAuthHeaders)
            {
                return false;
            }

            return context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
