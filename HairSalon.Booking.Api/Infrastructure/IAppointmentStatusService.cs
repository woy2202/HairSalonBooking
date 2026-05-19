using HairSalon.Booking.Core.Models;

namespace HairSalon.Booking.Api.Infrastructure
{
    public interface IAppointmentStatusService
    {
        Task RefreshExpiredAppointmentsAsync(CancellationToken cancellationToken);
        Task<Appointment> RefreshExpiredAppointmentAsync(Appointment appointment, CancellationToken cancellationToken);
    }
}
