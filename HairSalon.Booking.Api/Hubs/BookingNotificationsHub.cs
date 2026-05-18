using HairSalon.Booking.Api.Infrastructure;
using HairSalon.Booking.Core.Models;
using HairSalon.Booking.Core.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace HairSalon.Booking.Api.Hubs
{
    public sealed class BookingNotificationsHub : Hub
    {
        private const string AdminGroupName = "administratorzy";
        private readonly IBookingRepository<AppUser> _users;

        public BookingNotificationsHub(IBookingRepository<AppUser> users)
        {
            _users = users;
        }

        public async Task JoinHairdresserGroup(string hairdresserId)
        {
            if (!await CanJoinHairdresserGroupAsync(hairdresserId))
            {
                throw new HubException("Nie masz uprawnień do dołączenia do tej grupy fryzjera.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"hairdresser:{hairdresserId}");
        }

        public async Task JoinAdminGroup()
        {
            if (!await IsAdminAsync())
            {
                throw new HubException("Tylko administrator może dołączyć do grupy administratorów.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroupName);
        }

        public Task LeaveHairdresserGroup(string hairdresserId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"hairdresser:{hairdresserId}");
        }

        public static string GetAdminGroupName()
        {
            return AdminGroupName;
        }

        private async Task<bool> CanJoinHairdresserGroupAsync(string hairdresserId)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext is null)
            {
                return false;
            }

            var provider = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].ToString();
            var providerUserId = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].ToString();
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId))
            {
                return false;
            }

            var principal = new CurrentUserInfo(provider, providerUserId, string.Empty, string.Empty);
            var user = await _users.GetAsync(principal.LocalUserId, Context.ConnectionAborted);

            if (user?.Role == UserRole.Admin)
            {
                return true;
            }

            return user?.Role == UserRole.Hairdresser && user.HairdresserId == hairdresserId;
        }

        private async Task<bool> IsAdminAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Role == UserRole.Admin;
        }

        private async Task<AppUser?> GetCurrentUserAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext is null)
            {
                return null;
            }

            var provider = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].ToString();
            var providerUserId = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].ToString();
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId))
            {
                return null;
            }

            var principal = new CurrentUserInfo(provider, providerUserId, string.Empty, string.Empty);
            return await _users.GetAsync(principal.LocalUserId, Context.ConnectionAborted);
        }
    }
}
