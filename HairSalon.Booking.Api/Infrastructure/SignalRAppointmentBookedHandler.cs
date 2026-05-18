using HairSalon.Booking.Api.Hubs;
using HairSalon.Booking.Core.Events;
using HairSalon.Booking.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class SignalRAppointmentBookedHandler : IAppointmentBookedHandler
    {
        private readonly IHubContext<BookingNotificationsHub> _hubContext;

        public SignalRAppointmentBookedHandler(IHubContext<BookingNotificationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

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

            await _hubContext.Clients.Group(BookingNotificationsHub.GetAdminGroupName())
                .SendAsync("appointmentBooked", payload, cancellationToken);
            await _hubContext.Clients.Group($"hairdresser:{appointment.HairdresserId}")
                .SendAsync("hairdresserAppointmentBooked", payload, cancellationToken);
        }
    }
}
