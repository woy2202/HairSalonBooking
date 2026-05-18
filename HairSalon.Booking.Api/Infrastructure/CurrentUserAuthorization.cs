using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Infrastructure
{
    public static class CurrentUserAuthorization
    {
        public static async Task<bool> IsAdminAsync(this ICurrentUserService currentUser, CancellationToken cancellationToken)
        {
            var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
            return user?.Role == UserRole.Admin;
        }

        public static async Task<bool> IsCustomerAsync(this ICurrentUserService currentUser, CancellationToken cancellationToken)
        {
            var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
            return user?.Role == UserRole.Customer && !string.IsNullOrWhiteSpace(user.CustomerId);
        }

        public static async Task<bool> IsHairdresserAsync(this ICurrentUserService currentUser, CancellationToken cancellationToken)
        {
            var user = await currentUser.GetCurrentAppUserAsync(cancellationToken);
            return user?.Role == UserRole.Hairdresser && !string.IsNullOrWhiteSpace(user.HairdresserId);
        }
    }
}
