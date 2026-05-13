using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Infrastructure;

public interface ICurrentUserService
{
    CurrentUserInfo? GetCurrentPrincipal();
    Task<AppUser?> GetCurrentAppUserAsync(CancellationToken cancellationToken);
}
