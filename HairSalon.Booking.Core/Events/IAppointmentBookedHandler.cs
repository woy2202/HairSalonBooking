using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Core.Events
{
    public interface IAppointmentBookedHandler
    {
        Task HandleAsync(Appointment appointment, CancellationToken cancellationToken);
    }
}
