using HairSalon.Booking.Api.Hubs;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace HairSalon.Booking.Api.Infrastructure;

public sealed class SignalRAppointmentBookedHandler(IHubContext<BookingNotificationsHub> hubContext) : IAppointmentBookedHandler
{
    public async Task HandleAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var payload = new
        {
            appointmentId = appointment.id,
            appointment.CustomerId,
            appointment.HairdresserId,
            appointment.SalonServiceId,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status
        };

        await hubContext.Clients.All.SendAsync("appointmentBooked", payload, cancellationToken);
        await hubContext.Clients.Group($"hairdresser:{appointment.HairdresserId}")
            .SendAsync("hairdresserAppointmentBooked", payload, cancellationToken);
    }
}
