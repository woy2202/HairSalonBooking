using Microsoft.AspNetCore.SignalR;

namespace HairSalon.Booking.Api.Hubs;

public sealed class BookingNotificationsHub : Hub
{
    public Task JoinHairdresserGroup(string hairdresserId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"hairdresser:{hairdresserId}");
    }

    public Task LeaveHairdresserGroup(string hairdresserId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"hairdresser:{hairdresserId}");
    }
}
